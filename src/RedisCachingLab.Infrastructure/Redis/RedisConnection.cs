using StackExchange.Redis;

namespace RedisCachingLab.Infrastructure.Redis;

public sealed class RedisConnection
{
    public ConnectionMultiplexer Connection { get; }

    public RedisConnection(string connectionString)
    {
        var options = ConfigurationOptions.Parse(connectionString);

        options.AbortOnConnectFail = false;

        // Maximum time to wait for a Redis operation
        options.AsyncTimeout = 1000;

        // Time allowed for initial connection
        options.ConnectTimeout = 1000;

        // Don't queue commands while Redis is disconnected
        options.BacklogPolicy = BacklogPolicy.FailFast;

        Connection = ConnectionMultiplexer.Connect(options);
    }
}
