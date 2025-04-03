using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
using FoxIDs.Models.Logic;

namespace FoxIDs.Logic
{
    public class SamlAuthnUpIdPInitiatedGrantLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public SamlAuthnUpIdPInitiatedGrantLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task CreateGrantAsync(SamlUpParty party, string sessionId, List<Claim> claims, IdPInitiatedDownPartyLink idPInitiatedLink)
        {
            logger.ScopeTrace(() => $"Create IdP-Initiated login grant, Route '{RouteBinding.Route}'.");

            if (sessionId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(sessionId));

            var grant = new SamlUpPartyIdPInitiatedTtlGrant
            {
                TimeToLive = party.IdPInitiatedGrantLifetime.Value,
                DownPartyId = idPInitiatedLink.DownPartyId,
                DownPartyType = idPInitiatedLink.DownPartyType,
            };
            grant.Claims = new List<ClaimAndValues>();
            foreach (var gc in claims.ToClaimAndValues())
            {
                try
                {
                    await gc.ValidateObjectAsync();
                    grant.Claims.Add(gc);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, $"Unable to save claim '{gc.Claim}' in IdP-Initiated login grant.");
                }
            }
            var code = await GetCodeAsync(party, sessionId);
            await grant.SetIdAsync(new SamlUpPartyIdPInitiatedTtlGrant.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Code = code });
            await tenantDataRepository.SaveAsync(grant);

            logger.ScopeTrace(() => $"IdP-Initiated login grant, Code '{code}'.");
        }

        private Task<string> GetCodeAsync(SamlUpParty party, string sessionId)
        {
            return $"{party.Name}-{sessionId}".HashIdStringAsync();
        }

        public async Task<SamlUpPartyIdPInitiatedTtlGrant> GetGrantAsync(SamlUpParty party, string sessionId)
        {
            var code = await GetCodeAsync(party, sessionId);
            logger.ScopeTrace(() => $"Get IdP-Initiated login grant, Route '{RouteBinding.Route}', Code '{code}'.");

            var grantIdKey = new SamlUpPartyIdPInitiatedTtlGrant.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Code = code };
            await grantIdKey.ValidateObjectAsync();

            var grant = await tenantDataRepository.GetAsync<SamlUpPartyIdPInitiatedTtlGrant>(await SamlUpPartyIdPInitiatedTtlGrant.IdFormatAsync(grantIdKey), required: false, delete: true);
            logger.ScopeTrace(() => $"IdP-Initiated login grant {(grant == null ? "not " : string.Empty)}found, Code '{code}'.");
            return grant;
        }

    }
}
