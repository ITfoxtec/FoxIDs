using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace FoxIDs.Logic
{
    public class OAuthRefreshTokenGrantDownBaseLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public OAuthRefreshTokenGrantDownBaseLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task<(IReadOnlyCollection<RefreshTokenTtlGrant> ttlGrants, IReadOnlyCollection<RefreshTokenGrant> grants, string paginationToken)> ListRefreshTokenGrantsByUserIdentifierAndClientIdAsync(string userIdentifier, string clientId, string paginationToken = null)
        {
            var queryByUserIdentifier = !userIdentifier.IsNullOrWhiteSpace();
            var queryByClientId = !clientId.IsNullOrWhiteSpace();

            (var ttlGrantsPaginationToken, var grantsPaginationToken) = GetGrantPaginationTokens(paginationToken);

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            (var ttlGrants, var nextTtlGrantsPaginationToken) = await tenantDataRepository.GetListAsync<RefreshTokenTtlGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) &&
                (!queryByUserIdentifier || d.Sub == userIdentifier || d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier) &&
                (!queryByClientId || d.ClientId == clientId),
                paginationToken: ttlGrantsPaginationToken);

            (var grants, var nextGrantsPaginationToken) = await tenantDataRepository.GetListAsync<RefreshTokenGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) &&
                (!queryByUserIdentifier || d.Sub == userIdentifier || d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier) &&
                (!queryByClientId || d.ClientId == clientId),
                paginationToken: grantsPaginationToken);

            return (ttlGrants, grants, CreateCombinedPaginationToken(nextTtlGrantsPaginationToken, nextGrantsPaginationToken));
        }

        public async Task<(RefreshTokenTtlGrant ttlGrant, RefreshTokenGrant grant)> GetRefreshTokenGrantsByUserIdentifierAndClientIdAsync(string userIdentifier, string clientId)
        {
            if (userIdentifier.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(userIdentifier));
            }

            var queryByUserIdentifier = !userIdentifier.IsNullOrWhiteSpace();
            var queryByClientId = !clientId.IsNullOrWhiteSpace();

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            (var ttlGrants, _) = await tenantDataRepository.GetListAsync<RefreshTokenTtlGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) &&
                (!queryByUserIdentifier || d.Sub == userIdentifier || d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier) &&
                (!queryByClientId || d.ClientId == clientId),
                pageSize: 1);

            (var grants, _) = await tenantDataRepository.GetListAsync<RefreshTokenGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) &&
                (!queryByUserIdentifier || d.Sub == userIdentifier || d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier) &&
                (!queryByClientId || d.ClientId == clientId),
            pageSize: 1);
            
            if (ttlGrants.Count() > 0 || grants.Count() > 0)
            {
                return (ttlGrants.FirstOrDefault(), grants.FirstOrDefault());
            }
            else
            {
                throw new FoxIDsDataException(DataDocument.PartitionIdFormat(idKey)) { StatusCode = DataStatusCode.NotFound };
            }
        }

        public async Task DeleteRefreshTokenGrantsByUserIdentifierAndClientIdAsync(string userIdentifier, string clientId = null)
        {
            if (userIdentifier.IsNullOrWhiteSpace()) return;

            var queryByUserIdentifier = !userIdentifier.IsNullOrWhiteSpace();
            var queryByClientId = !clientId.IsNullOrWhiteSpace();

            logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', User identifier '{userIdentifier}', Client ID '{clientId}'.");

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            var ttlGrantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenTtlGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) &&
                (!queryByUserIdentifier || d.Sub == userIdentifier || d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier) &&
                (!queryByClientId || d.ClientId == clientId));
            if (ttlGrantCount > 0)
            {
                logger.ScopeTrace(() => $"TTL Refresh Token grants deleted, User identifier '{userIdentifier}', Client ID '{clientId}'.");
            }

            var grantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) &&
                (!queryByUserIdentifier || d.Sub == userIdentifier || d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier) &&
                (!queryByClientId || d.ClientId == clientId));
            if (grantCount > 0)
            {
                logger.ScopeTrace(() => $"Refresh Token grants deleted, User identifier '{userIdentifier}', Client ID '{clientId}'.");
            }
        }

        //public async Task DeleteRefreshTokenGrantsBySubAsync(string sub)
        //{
        //    if (sub.IsNullOrWhiteSpace()) return;

        //    logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', Sub '{sub}'.");

        //    var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
        //    var ttlGrantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenTtlGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.Sub == sub);
        //    if (ttlGrantCount > 0)
        //    {
        //        logger.ScopeTrace(() => $"TTL Refresh Token grants deleted, Sub '{sub}'.");
        //    }
        //    var grantCount = await tenantDataRepository.DeleteListAsync<RefreshTokenGrant>(idKey, d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) && d.Sub == sub);
        //    if (grantCount > 0)
        //    {
        //        logger.ScopeTrace(() => $"Refresh Token grants deleted, Sub '{sub}'.");
        //    }
        //}

        private (string ttlGrantsPaginationToken, string grantsPaginationToken) GetGrantPaginationTokens(string paginationToken)
        {
            if (!paginationToken.IsNullOrWhiteSpace())
            {
                var ptSplit = paginationToken.Split('&');
                if (ptSplit.Count() == 2)
                {
                    var ttlGrantsPaginationToken = ptSplit[0];
                    var grantsPaginationToken = ptSplit[1];

                    return (ttlGrantsPaginationToken.IsNullOrWhiteSpace() ? null : HttpUtility.UrlDecode(ttlGrantsPaginationToken), grantsPaginationToken.IsNullOrWhiteSpace() ? null : HttpUtility.UrlDecode(grantsPaginationToken));
                }
            }

            return (null, null);
        }

        private string CreateCombinedPaginationToken(string ttlGrantsPaginationToken, string grantsPaginationToken)
        {
            if (ttlGrantsPaginationToken.IsNullOrWhiteSpace() && grantsPaginationToken.IsNullOrWhiteSpace())
            {
                return null;
            }

            return $"{(ttlGrantsPaginationToken.IsNullOrWhiteSpace() ? string.Empty : HttpUtility.UrlEncode(ttlGrantsPaginationToken))}&{(grantsPaginationToken.IsNullOrWhiteSpace() ? string.Empty : HttpUtility.UrlEncode(grantsPaginationToken))}";
        }
    }
}