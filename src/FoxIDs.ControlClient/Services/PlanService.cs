using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class PlanService : BaseService
    {
        private const string apiUri = "api/@master/!plan";
        private const string listApiUri = "api/@master/!plans";

        public PlanService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<Plan>> GetPlansAsync(string filterValue, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<Plan>(listApiUri, filterValue, paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<Plan> GetPlanAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<Plan>(apiUri, name, cancellationToken: cancellationToken);
        public async Task<Plan> CreatePlanAsync(Plan plan, CancellationToken cancellationToken = default) => await PostResponseAsync<Plan, Plan>(apiUri, plan, cancellationToken);
        public async Task<Plan> UpdatePlanAsync(Plan plan, CancellationToken cancellationToken = default) => await PutResponseAsync<Plan, Plan>(apiUri, plan, cancellationToken);
        public async Task DeletePlanAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(apiUri, name, cancellationToken: cancellationToken);
    }
}
