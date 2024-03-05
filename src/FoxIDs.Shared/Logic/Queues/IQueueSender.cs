using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic;

public interface IQueueSender : IAsyncDisposable
{
    public Task SendAsync(string message);
}