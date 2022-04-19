using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;

namespace FoxIDs.Repository
{
    public class RepositoryClient : RepositoryClientBase, IRepositoryClient
    { 
        public RepositoryClient(Settings settings, TelemetryLogger logger) : base (settings, logger, true, false)
        { }
    }
}
