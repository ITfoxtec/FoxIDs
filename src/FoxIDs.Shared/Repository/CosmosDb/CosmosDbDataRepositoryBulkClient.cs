using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;

namespace FoxIDs.Repository
{
    public class CosmosDbDataRepositoryBulkClient : CosmosDbDataRepositoryClientBase, ICosmosDbDataRepositoryBulkClient
    { 
        public CosmosDbDataRepositoryBulkClient(Settings settings, TelemetryLogger logger) : base (settings, logger, false, true)
        { }
    }
}
