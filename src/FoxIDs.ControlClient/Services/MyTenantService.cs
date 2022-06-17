using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class MyTenantService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!mytenant";

        public MyTenantService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }
        
        public async Task<Tenant> GetTenantAsync() => await GetAsync<Tenant>(apiUri);
        public async Task<Tenant> UpdateTenantAsync(MyTenantRequest tenant) => await PutResponseAsync<MyTenantRequest, Tenant>(apiUri, tenant);
        public async Task DeleteTenantAsync() => await DeleteAsync(apiUri);
    }
}
