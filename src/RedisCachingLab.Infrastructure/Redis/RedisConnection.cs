using StackExchange.Redis;

namespace RedisCachingLab.Infrastructure.Redis;

public sealed class RedisConnection
{
    public ConnectionMultiplexer Connection { get; }

    public RedisConnection(string connectionString)
    {
        Connection = ConnectionMultiplexer.Connect(connectionString);
    }
}
