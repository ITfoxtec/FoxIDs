using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic;

public interface IQueueProcessor : IAsyncDisposable
{
    event Func<string, CancellationToken, Task> ProcessAsync;
}