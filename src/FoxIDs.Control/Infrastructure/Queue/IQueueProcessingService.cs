using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Queue
{
    public interface IQueueProcessingService
    {
        Task DoWorkAsync(string tenantName, string trackName, string message, CancellationToken stoppingToken);
    }
}
