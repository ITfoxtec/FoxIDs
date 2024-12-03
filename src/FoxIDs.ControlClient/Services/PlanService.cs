using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class PlanService : BaseService
    {
        private const string apiUri = "api/@master/!plan";
        private const string listApiUri = "api/@master/!plans";

        public PlanService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<Plan>> GetPlansAsync(string filterValue, string paginationToken = null) => await GetListAsync<Plan>(listApiUri, filterValue, paginationToken: paginationToken);

        public async Task<Plan> GetPlanAsync(string name) => await GetAsync<Plan>(apiUri, name);
        public async Task<Plan> CreatePlanAsync(Plan plan) => await PostResponseAsync<Plan, Plan>(apiUri, plan);
        public async Task<Plan> UpdatePlanAsync(Plan plan) => await PutResponseAsync<Plan, Plan>(apiUri, plan);
        public async Task DeletePlanAsync(string name) => await DeleteAsync(apiUri, name);
    }
}
