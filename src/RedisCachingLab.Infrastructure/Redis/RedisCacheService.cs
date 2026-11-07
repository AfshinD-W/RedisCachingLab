using Microsoft.Extensions.Logging;
using RedisCachingLab.Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace RedisCachingLab.Infrastructure.Redis
{
    public sealed class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(RedisConnection redisConnection, ILogger<RedisCacheService> logger)
        {
            _database = redisConnection.Connection.GetDatabase();
            _logger = logger;
        }


        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            string json = JsonSerializer.Serialize(value);

            try
            {
                await _database.StringSetAsync(key, json, expiry, false);
            }
            catch (Exception ex) when (IsRedisConnectionError(ex))
            {
                _logger.LogWarning(ex, "Redis SET failed for key: {CacheKey}", key);
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                RedisValue value = await _database.StringGetAsync(key);

                if (value.IsNull)
                    return default;

                return JsonSerializer.Deserialize<T>(value.ToString());
            }
            catch (Exception ex) when (IsRedisConnectionError(ex))
            {
                _logger.LogWarning(ex, "Redis GET failed for key: {CacheKey}", key);

                return default;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
            }
            catch (Exception ex) when (IsRedisConnectionError(ex))
            {
                _logger.LogWarning(ex, "Redis DELETE failed for key: {CacheKey}", key);
            }
        }


        public async Task SetHashAsync<T>(string key, T value)
        {
            var properties = typeof(T).GetProperties();

            var entries = properties.Select(property =>
                    new HashEntry(
                        property.Name,
                        property.GetValue(value)?.ToString() ?? string.Empty
                        )
                    ).ToArray();

            await _database.HashSetAsync(key, entries);
        }

        public async Task<T?> GetHashAsync<T>(string key) where T : new()
        {
            HashEntry[] entries = await _database.HashGetAllAsync(key);

            if (entries.Length == 0)
                return default;

            var result = new T();

            var properties = typeof(T).GetProperties();


            foreach (var entry in entries)
            {
                var property = properties.FirstOrDefault(
                    p => p.Name.Equals(
                        entry.Name.ToString(),
                        StringComparison.OrdinalIgnoreCase
                        )
                    );


                if (property is null || !property.CanWrite)
                    continue;


                object? convertedValue = Convert.ChangeType(
                    entry.Value.ToString(),
                    Nullable.GetUnderlyingType(property.PropertyType)
                        ?? property.PropertyType
                    );

                property.SetValue(result, convertedValue);
            }

            return result;
        }

        public async Task SetHashFieldAsync(string key, string field, RedisValue value)
            => await _database.HashSetAsync(key, field, value);

        public async Task<RedisValue> GetHashFieldAsync(string key, string field)
            => await _database.HashGetAsync(key, field);


        private bool IsRedisConnectionError(Exception exception)
        {
            return exception is RedisConnectionException or RedisTimeoutException;
        }
    }
}
