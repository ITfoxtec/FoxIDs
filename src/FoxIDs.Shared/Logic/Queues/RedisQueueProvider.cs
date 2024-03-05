using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace FoxIDs.Logic;

public class RedisQueueProvider(IConnectionMultiplexer redisConnectionMultiplexer) : IQueueProvider
{
    public async Task<IQueueProcessor> CreateProcessorAsync(string queue)
    {
        var sub = redisConnectionMultiplexer.GetSubscriber();
        var channel = await sub.SubscribeAsync(queue);
        return new RedisQueueProcessor(redisConnectionMultiplexer, queue, channel);
    }

    public async Task<IQueueSender> CreateSenderAsync(string queue)
    {
        return new RedisQueueSender(redisConnectionMultiplexer, queue);
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}