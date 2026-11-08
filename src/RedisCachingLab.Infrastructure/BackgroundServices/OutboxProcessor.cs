using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisCachingLab.Core.Entities;
using RedisCachingLab.Core.Events;
using RedisCachingLab.Core.Exceptions;
using RedisCachingLab.Core.Interfaces;
using RedisCachingLab.Infrastructure.Persistence;
using System.Text.Json;

namespace RedisCachingLab.Infrastructure.BackgroundServices;

public class OutboxProcessor(IServiceScopeFactory scopeFactory, ICacheService cache, ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();


                var now = DateTime.UtcNow;

                var messages = await context.OutboxMessages
                    .Where(x =>
                        x.ProcessedAt == null &&
                        x.FailedAt == null &&
                        (x.NextAttemptAt == null || x.NextAttemptAt <= now))
                    .OrderBy(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Take(50)
                    .ToListAsync(stoppingToken);


                foreach (var message in messages)
                {
                    await ProcessMessageAsync(message, context, stoppingToken);
                }

                if (messages.Count > 0)
                {
                    logger.LogInformation("Found {Count} pending Outbox messages.", messages.Count);
                }

            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing Outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }


    private async Task ProcessMessageAsync(OutboxMessage message, AppDbContext context, CancellationToken stoppingToken)
    {
        try
        {
            switch (message.Type)
            {
                case "ProductUpdated":
                    {
                        var payload = JsonSerializer.Deserialize<ProductUpdatedPayload>(message.Payload);

                        if (payload is null || payload.ProductId <= 0)
                            throw new PermanentOutboxException("Invalid ProductUpdated payload.");

                        await cache.RemoveStrictAsync($"products:{payload.ProductId}");

                        await cache.RemoveStrictAsync("products:all");

                        break;
                    }

                default:
                    throw new PermanentOutboxException($"Unknown Outbox message type: {message.Type}");
            }

            message.ProcessedAt = DateTime.UtcNow;
            message.LastError = null;
            message.NextAttemptAt = null;

            await context.SaveChangesAsync(stoppingToken);

            logger.LogInformation("Outbox message {MessageId} processed successfully.", message.Id);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PermanentOutboxException ex)
        {
            message.ProcessedAt = null;
            message.FailedAt = DateTime.UtcNow;
            message.NextAttemptAt = null;
            message.LastError = ex.Message;

            await context.SaveChangesAsync(stoppingToken);

            logger.LogError(
                ex,
                "Outbox message {MessageId} permanently failed. No retry will be attempted.",
                message.Id);
        }
        catch (Exception ex)
        {
            message.ProcessedAt = null;
            message.RetryCount++;
            message.LastError = ex.Message;

            const int maxRetryCount = 5;

            if (message.RetryCount >= maxRetryCount)
            {
                message.FailedAt = DateTime.UtcNow;
                message.NextAttemptAt = null;

                logger.LogError(ex, "Outbox message {MessageId} permanently failed after {RetryCount} attempts.", message.Id, message.RetryCount);
            }
            else
            {
                message.NextAttemptAt = DateTime.UtcNow.Add(CalculateRetryDelay(message.RetryCount));

                logger.LogWarning(ex, "Outbox message {MessageId} failed. Retry count: {RetryCount}. Next attempt: {NextAttemptAt}", message.Id, message.RetryCount, message.NextAttemptAt);
            }

            await context.SaveChangesAsync(stoppingToken);

            logger.LogError(ex, "Failed to process Outbox message {MessageId}. Retry count: {RetryCount}", message.Id, message.RetryCount);
        }
    }


    private static TimeSpan CalculateRetryDelay(int retryCount)
    {
        var delaySeconds = Math.Min(
            10 * Math.Pow(2, retryCount - 1),
            300);

        return TimeSpan.FromSeconds(delaySeconds);
    }

}
