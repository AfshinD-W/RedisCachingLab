using StackExchange.Redis;

namespace RedisCachingLab.Infrastructure.Redis;

public sealed class RedisConnection
{
    private readonly ConnectionMultiplexer _Connection;

    public RedisConnection(string connectionString)
    {
        _Connection = ConnectionMultiplexer.Connect(connectionString);
    }
}
