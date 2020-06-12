using FoxIDs.Logic;
using System.Threading.Tasks;

namespace FoxIDs.Services
{
    public abstract class BaseService
    {
        public BaseService(RouteBindingLogic routeBindingLogic)
        {
            RouteBindingLogic = routeBindingLogic;
        }

        public RouteBindingLogic RouteBindingLogic { get; }

        protected async Task<string> GetTenantApiUrlAsync(string url, string tenantName = null)
        {
            tenantName = tenantName ?? await RouteBindingLogic.GetTenantNameAsync();
            return url.Replace("{tenant}", tenantName);
        }
    }
}
