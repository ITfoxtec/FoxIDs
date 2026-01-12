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

        public async Task<PaginationResponse<ExternalUser>> GetExternalUsersAsync(string filterValue, string filterClaimValue, string paginationToken = null) => await GetListAsync<ExternalUser>(listApiUri, filterValue, parmValue2: filterClaimValue, parmName1: nameof(filterValue), parmName2: nameof(filterClaimValue), paginationToken: paginationToken);

        public async Task<ExternalUser> GetExternalUserAsync(string upPartyName, string linkClaimValue, string redemptionClaimValue) => await GetAsync<ExternalUserId, ExternalUser>(apiUri, new ExternalUserId { UpPartyName = upPartyName, LinkClaimValue = linkClaimValue, RedemptionClaimValue = redemptionClaimValue });
        public async Task<ExternalUser> CreateExternalUserAsync(ExternalUserRequest externalUser) => await PostResponseAsync<ExternalUserRequest, ExternalUser>(apiUri, externalUser);
        public async Task<ExternalUser> UpdateExternalUserAsync(ExternalUserUpdateRequest user) => await PutResponseAsync<ExternalUserUpdateRequest, ExternalUser>(apiUri, user);
        public async Task DeleteExternalUserAsync(string upPartyName, string linkClaim, string redemptionClaimValue) => await DeleteByRequestObjAsync(apiUri, new ExternalUserId { UpPartyName = upPartyName, LinkClaimValue = linkClaim, RedemptionClaimValue = redemptionClaimValue });
    }
}
