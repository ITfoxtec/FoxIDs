using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic;

public interface IQueueProvider : IAsyncDisposable
{
    public Task<IQueueSender> CreateSenderAsync(string queue);
    public Task<IQueueProcessor> CreateProcessorAsync(string queue);
}