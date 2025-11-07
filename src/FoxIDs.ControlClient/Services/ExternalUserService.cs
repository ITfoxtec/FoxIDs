using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class ExternalUserService : BaseService
    {
        private const string apiUri = "api/{tenant}/{track}/!externaluser";
        private const string listApiUri = "api/{tenant}/{track}/!externalusers";

        public ExternalUserService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<ExternalUser>> GetExternalUsersAsync(string filterValue, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<ExternalUser>(listApiUri, filterValue, parmName1: nameof(filterValue), paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<ExternalUser> GetExternalUserAsync(string upPartyName, string linkClaimValue, string redemptionClaimValue, CancellationToken cancellationToken = default) => await GetAsync<ExternalUserId, ExternalUser>(apiUri, new ExternalUserId { UpPartyName = upPartyName, LinkClaimValue = linkClaimValue, RedemptionClaimValue = redemptionClaimValue }, cancellationToken);
        public async Task<ExternalUser> CreateExternalUserAsync(ExternalUserRequest externalUser, CancellationToken cancellationToken = default) => await PostResponseAsync<ExternalUserRequest, ExternalUser>(apiUri, externalUser, cancellationToken);
        public async Task<ExternalUser> UpdateExternalUserAsync(ExternalUserUpdateRequest user, CancellationToken cancellationToken = default) => await PutResponseAsync<ExternalUserUpdateRequest, ExternalUser>(apiUri, user, cancellationToken);
        public async Task DeleteExternalUserAsync(string upPartyName, string linkClaim, string redemptionClaimValue, CancellationToken cancellationToken = default) => await DeleteByRequestObjAsync(apiUri, new ExternalUserId { UpPartyName = upPartyName, LinkClaimValue = linkClaim, RedemptionClaimValue = redemptionClaimValue }, cancellationToken);
    }
}
