using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public async Task<(RefreshTokenTtlGrant ttlGrant, RefreshTokenGrant grant)> GetRefreshTokenGrantAsync(string refreshToken)
        {
            if (refreshToken.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(refreshToken));
            }

            var idKey = new RefreshTokenGrant.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, RefreshToken = refreshToken };
            var id = await RefreshTokenGrant.IdFormatAsync(idKey);
            var ttlGrant = await tenantDataRepository.GetAsync<RefreshTokenTtlGrant>(id, required: false);
            var grant = await tenantDataRepository.GetAsync<RefreshTokenGrant>(id, required: false);

            if (ttlGrant == null && grant == null)
            {
                throw new FoxIDsDataException(id, DataDocument.PartitionIdFormat(idKey)) { StatusCode = DataStatusCode.NotFound };
            }

            return (ttlGrant, grant);
        }

        public async Task<(IReadOnlyCollection<RefreshTokenTtlGrant> ttlGrants, IReadOnlyCollection<RefreshTokenGrant> grants, string paginationToken)> ListRefreshTokenGrantsAsync(string userIdentifier, string sub, string clientId, string upPartyName, string paginationToken = null)
        {
            (var ttlGrantsPaginationToken, var grantsPaginationToken) = GetGrantPaginationTokens(paginationToken);

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            (var ttlGrants, var nextTtlGrantsPaginationToken) = await tenantDataRepository.GetManyAsync(idKey, GetQuery<RefreshTokenTtlGrant>(userIdentifier, sub, clientId, upPartyName), paginationToken: ttlGrantsPaginationToken);
            (var grants, var nextGrantsPaginationToken) = await tenantDataRepository.GetManyAsync(idKey, GetQuery<RefreshTokenGrant>(userIdentifier, sub, clientId, upPartyName), paginationToken: grantsPaginationToken);

            return (ttlGrants, grants, CreateCombinedPaginationToken(nextTtlGrantsPaginationToken, nextGrantsPaginationToken));
        }

        public async Task DeleteRefreshTokenGrantAsync(string refreshToken)
        {
            if (refreshToken.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(refreshToken));
            }

            (var ttlGrant, var grant) = await GetRefreshTokenGrantAsync(refreshToken);

            logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', Refresh token '{refreshToken}'.");

            if (ttlGrant != null)
            {
                await tenantDataRepository.DeleteAsync<RefreshTokenTtlGrant>(ttlGrant.Id);
                logger.ScopeTrace(() => $"TTL Refresh Token grants deleted.");
            }
            
            if(grant != null)
            {
                await tenantDataRepository.DeleteAsync<RefreshTokenGrant>(grant.Id);
                logger.ScopeTrace(() => $"Refresh Token grants deleted.");
            }
        }

        public async Task DeleteRefreshTokenGrantsAsync(string userIdentifier, string sub = null, string clientId = null, string upPartyName = null, PartyTypes? upPartyType = null)
        {
            if (userIdentifier.IsNullOrWhiteSpace() && sub.IsNullOrWhiteSpace() && clientId.IsNullOrWhiteSpace() && upPartyName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException($"Either the {nameof(userIdentifier)} or the {nameof(sub)} or the {nameof(clientId)} or the {nameof(upPartyName)} parameter is required.");
            }

            logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', User identifier '{userIdentifier}', Sub '{sub}', Client ID '{clientId}', Auth method '{upPartyName}'.");

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            var ttlGrantCount = await tenantDataRepository.DeleteManyAsync(idKey, GetQuery<RefreshTokenTtlGrant>(userIdentifier, sub, clientId, upPartyName, upPartyType));
            if (ttlGrantCount > 0)
            {
                logger.ScopeTrace(() => $"TTL Refresh Token grants deleted.");
            }

            var grantCount = await tenantDataRepository.DeleteManyAsync(idKey, GetQuery<RefreshTokenGrant>(userIdentifier, sub, clientId, upPartyName, upPartyType));
            if (grantCount > 0)
            {
                logger.ScopeTrace(() => $"Refresh Token grants deleted.");
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

        private static Expression<Func<T, bool>> GetQuery<T>(string userIdentifier, string sub, string clientId, string upPartyName, PartyTypes? upPartyType = null) where T : RefreshTokenGrant
        {
            var queryByUserIdentifier = !userIdentifier.IsNullOrWhiteSpace();
            var queryBySub = !sub.IsNullOrWhiteSpace();
            var queryByClientId = !clientId.IsNullOrWhiteSpace();
            var queryByUpPartyName = !upPartyName.IsNullOrWhiteSpace();
            var queryByUpPartyType = upPartyType.HasValue;
            var upPartyTypeValue = upPartyType.HasValue ? upPartyType.Value.GetPartyTypeValue() : null;

            return d =>
                d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) &&
                (
                    (!queryByUserIdentifier && !queryBySub) || // none provided
                    (queryByUserIdentifier && !queryBySub && (d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier)) || // only userIdentifier
                    (!queryByUserIdentifier && queryBySub && d.Sub == sub) || // only sub
                    (queryByUserIdentifier && queryBySub && (d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier || d.Sub == sub)) // both => OR
                ) &&
                (!queryByClientId || d.ClientId == clientId) &&
                (!queryByUpPartyName || d.UpPartyName == upPartyName) &&
                (!queryByUpPartyType || d.UpPartyType == upPartyTypeValue);
        }
    }
}