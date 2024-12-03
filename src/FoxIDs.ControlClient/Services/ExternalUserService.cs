using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class ExternalUserService : BaseService
    {
        private const string apiUri = "api/{tenant}/{track}/!externaluser";
        private const string listApiUri = "api/{tenant}/{track}/!externalusers";

        public ExternalUserService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<ExternalUser>> GetExternalUsersAsync(string filterValue, string paginationToken = null) => await GetListAsync<ExternalUser>(listApiUri, filterValue, parmName1: nameof(filterValue), paginationToken: paginationToken);

        public async Task<ExternalUser> GetExternalUserAsync(string upPartyName, string linkClaim) => await GetAsync<ExternalUserId, ExternalUser>(apiUri, new ExternalUserId { UpPartyName = upPartyName, LinkClaimValue = linkClaim });
        public async Task<ExternalUser> CreateExternalUserAsync(ExternalUserRequest externalUser) => await PostResponseAsync<ExternalUserRequest, ExternalUser>(apiUri, externalUser);
        public async Task<ExternalUser> UpdateExternalUserAsync(ExternalUserRequest user) => await PutResponseAsync<ExternalUserRequest, ExternalUser>(apiUri, user);
        public async Task DeleteExternalUserAsync(string upPartyName, string linkClaim) => await DeleteByRequestObjAsync(apiUri, new ExternalUserId { UpPartyName = upPartyName, LinkClaimValue = linkClaim });
    }
}
