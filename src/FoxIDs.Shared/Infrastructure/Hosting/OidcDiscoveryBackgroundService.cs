using ITfoxtec.Identity.Discovery;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class OidcDiscoveryBackgroundService : BackgroundService
    {
        private readonly OidcDiscoveryHandlerService oidcDiscoveryHandlerService;

        public OidcDiscoveryBackgroundService(OidcDiscoveryHandlerService oidcDiscoveryHandlerService)
        {
            this.oidcDiscoveryHandlerService = oidcDiscoveryHandlerService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return oidcDiscoveryHandlerService.ExecuteAsync(stoppingToken);
        }
    }
}
