using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Queue
{
    public interface IQueueProcessingService
    {
        Task DoWorkAsync(string message, CancellationToken stoppingToken);
    }
}
