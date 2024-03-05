using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic;

public interface IQueueProcessor : IAsyncDisposable
{
    event Func<string, Task> ProcessAsync;
}