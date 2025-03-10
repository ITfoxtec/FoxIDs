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

        public async Task<(IReadOnlyCollection<RefreshTokenTtlGrant> ttlGrants, IReadOnlyCollection<RefreshTokenGrant> grants, string paginationToken)> ListRefreshTokenGrantsAsync(string userIdentifier, string clientId, string autoMethod, string paginationToken = null)
        {
            (var ttlGrantsPaginationToken, var grantsPaginationToken) = GetGrantPaginationTokens(paginationToken);

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            (var ttlGrants, var nextTtlGrantsPaginationToken) = await tenantDataRepository.GetListAsync(idKey, GetQuery<RefreshTokenTtlGrant>(userIdentifier, clientId, autoMethod), paginationToken: ttlGrantsPaginationToken);
            (var grants, var nextGrantsPaginationToken) = await tenantDataRepository.GetListAsync(idKey, GetQuery<RefreshTokenGrant>(userIdentifier, clientId, autoMethod), paginationToken: grantsPaginationToken);

            return (ttlGrants, grants, CreateCombinedPaginationToken(nextTtlGrantsPaginationToken, nextGrantsPaginationToken));
        }

        public async Task<(RefreshTokenTtlGrant ttlGrant, RefreshTokenGrant grant)> GetRefreshTokenGrantsAsync(string refreshToken)
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

        public async Task DeleteRefreshTokenGrantsAsync(string userIdentifier = null, string clientId = null, string authMethod = null)
        {
            if (userIdentifier.IsNullOrWhiteSpace() && clientId.IsNullOrWhiteSpace() && authMethod.IsNullOrWhiteSpace())
            {
                throw new ArgumentException($"Either the {nameof(userIdentifier)} or the {nameof(clientId)} or the {nameof(authMethod)} parameter is required.");
            }

            logger.ScopeTrace(() => $"Delete Refresh Token grants, Route '{RouteBinding.Route}', User identifier '{userIdentifier}', Client ID '{clientId}', Auth method '{authMethod}'.");

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            var ttlGrantCount = await tenantDataRepository.DeleteListAsync(idKey, GetQuery<RefreshTokenTtlGrant>(userIdentifier, clientId, authMethod));
            if (ttlGrantCount > 0)
            {
                logger.ScopeTrace(() => $"TTL Refresh Token grants deleted, User identifier '{userIdentifier}', Client ID '{clientId}', Auth method '{authMethod}'.");
            }

            var grantCount = await tenantDataRepository.DeleteListAsync(idKey, GetQuery<RefreshTokenGrant>(userIdentifier, clientId, authMethod));
            if (grantCount > 0)
            {
                logger.ScopeTrace(() => $"Refresh Token grants deleted, User identifier '{userIdentifier}', Client ID '{clientId}', Auth method '{authMethod}'.");
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

        private static Expression<Func<T, bool>> GetQuery<T>(string userIdentifier, string clientId, string authMethod) where T : RefreshTokenGrant
        {
            var queryByUserIdentifier = !userIdentifier.IsNullOrWhiteSpace();
            var queryByClientId = !clientId.IsNullOrWhiteSpace();
            var queryByAuthMethod = !authMethod.IsNullOrWhiteSpace();

            return d => d.DataType.Equals(Constants.Models.DataType.RefreshTokenGrant) &&
                            (!queryByUserIdentifier || d.Sub == userIdentifier || d.Email == userIdentifier || d.Phone == userIdentifier || d.Username == userIdentifier) &&
                            (!queryByClientId || d.ClientId == clientId) &&
                            (!queryByAuthMethod || d.AuthMethod == authMethod);
        }
    }
}