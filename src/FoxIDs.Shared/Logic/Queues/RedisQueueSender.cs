using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace FoxIDs.Logic;

public class RedisQueueSender(IConnectionMultiplexer redisConnectionMultiplexer, string queue) : IQueueSender
{
    string queuekey = queue.Replace("_event", "");

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public async Task SendAsync(string message)
    {
        var db = redisConnectionMultiplexer.GetDatabase();
        await db.ListLeftPushAsync(queuekey, message);
        var sub = redisConnectionMultiplexer.GetSubscriber();
        await sub.PublishAsync(queue, string.Empty);
    }
}