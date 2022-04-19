using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;

namespace FoxIDs.Repository
{
    public class RepositoryBulkClient : RepositoryClientBase, IRepositoryBulkClient
    { 
        public RepositoryBulkClient(Settings settings, TelemetryLogger logger) : base (settings, logger, false, true)
        { }
    }
}
