using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;

namespace FoxIDs.Repository
{
    public class CosmosDbDataRepositoryClient : CosmosDbDataRepositoryClientBase, ICosmosDbDataRepositoryClient
    { 
        public CosmosDbDataRepositoryClient(Settings settings, TelemetryLogger logger) : base (settings, logger, true, false)
        { }
    }
}
