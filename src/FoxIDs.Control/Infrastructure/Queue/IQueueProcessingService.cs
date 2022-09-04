using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Queue
{
    public interface IQueueProcessingService
    {
        Task DoWorkAsync(TelemetryScopedLogger scopedLogger, string tenantName, string trackName, string message, CancellationToken stoppingToken);
    }
}
