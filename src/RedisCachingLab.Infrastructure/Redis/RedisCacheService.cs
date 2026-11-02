using StackExchange.Redis;
using System.Text.Json;

namespace RedisCachingLab.Infrastructure.Redis
{
    public sealed class RedisCacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService(RedisConnection redisConnection)
        {
            _database = redisConnection.Connection.GetDatabase();
        }


        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            string json = JsonSerializer.Serialize(value);

            await _database.StringSetAsync(key, json, expiry, false);
        }

        public async Task<T?> GetTAsync<T>(string key)
        {
            RedisValue value = await _database.StringGetAsync(key);

            if (value.IsNull)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }

        public async Task RemoveAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
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
    }
}
