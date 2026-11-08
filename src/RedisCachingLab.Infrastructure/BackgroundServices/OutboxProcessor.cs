using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisCachingLab.Core.Entities;
using RedisCachingLab.Core.Events;
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
                            throw new InvalidOperationException("Invalid ProductUpdated payload.");

                        await cache.RemoveStrictAsync($"products:{payload.ProductId}");

                        await cache.RemoveStrictAsync("products:all");

                        break;
                    }

                default:
                    throw new NotSupportedException($"Unknown Outbox message type: {message.Type}");
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
        catch (Exception ex)
        {
            message.ProcessedAt = null;
            message.RetryCount++;
            message.LastError = ex.Message;
            message.NextAttemptAt = DateTime.UtcNow.AddSeconds(10);

            await context.SaveChangesAsync(stoppingToken);

            logger.LogError(ex, "Failed to process Outbox message {MessageId}. Retry count: {RetryCount}", message.Id, message.RetryCount);
        }
    }

}
