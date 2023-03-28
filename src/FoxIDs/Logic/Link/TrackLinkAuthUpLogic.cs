using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkAuthUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ClaimValidationLogic claimValidationLogic;

        public TrackLinkAuthUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, ClaimTransformLogic claimTransformLogic, ClaimValidationLogic claimValidationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.claimValidationLogic = claimValidationLogic;
        }

        public async Task<IActionResult> AuthRequestAsync(UpPartyLink partyLink, LoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "Up, Track link auth request.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantRepository.GetAsync<TrackLinkUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new TrackLinkUpSequenceData
            {
                KeyName = partyLink.Name,
                DownPartyLink = loginRequest.DownPartyLink,
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                LoginAction = loginRequest.LoginAction,
                UserId = loginRequest.UserId,
                MaxAge = loginRequest.MaxAge,
                LoginEmailHint = loginRequest.EmailHint,
                Acr = loginRequest.Acr,
            }, setKeyValidUntil: true);

            return HttpContext.GetTrackDownPartyUrl(party.ToDownTrackName, party.ToDownPartyName, party.SelectedUpParties, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkAuthRequest, includeKeySequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> AuthResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, Track link auth response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TrackLinkUpParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToDownTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkDownSequenceData>(keySequence, party.ToDownTrackName);
            if (party.ToDownPartyName != keySequenceData.KeyName)
            {
                throw new Exception($"Incorrect down-party name '{keySequenceData.KeyName}', expected down-party name '{party.ToDownPartyName}'.");
            }

            await sequenceLogic.ValidateAndSetSequenceAsync(keySequenceData.UpPartySequenceString);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>();

            List<Claim> claims = keySequenceData.Claims?.ToClaimList();
            if (keySequenceData.Error.IsNullOrEmpty())
            {
                var externalSessionId = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.SessionId);
                externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionIdMax, nameof(externalSessionId), "Session state or claim");
                claims = claims.Where(c => c.Type != JwtClaimTypes.SessionId && c.Type != Constants.JwtClaimTypes.UpParty && c.Type != Constants.JwtClaimTypes.UpPartyType).ToList();
                claims.AddClaim(Constants.JwtClaimTypes.UpParty, party.Name);
                claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, party.Type.ToString().ToLower());

                var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                claims = claimValidationLogic.ValidateUpPartyClaims(party.Claims, transformedClaims);
                logger.ScopeTrace(() => $"Up, Track link output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var sessionId = await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, party.DisableSingleLogout ? null : sequenceData.DownPartyLink, claims, externalSessionId);
                if (!sessionId.IsNullOrEmpty())
                {
                    claims.AddClaim(JwtClaimTypes.SessionId, sessionId);
                }

                if (!sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
                {
                    await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), PartyTypes.TrackLink);
                }
            }

            switch (sequenceData.DownPartyLink.Type)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    if (keySequenceData.Error.IsNullOrEmpty())
                    {
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyLink.Id, claims);
                    }
                    else
                    {
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyLink.Id, keySequenceData.Error, keySequenceData.ErrorDescription);
                    }
                case PartyTypes.Saml2:
                    var claimsLogic = serviceProvider.GetService<ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, SamlConvertLogic.ErrorToSamlStatus(keySequenceData.Error), claims != null ? await claimsLogic.FromJwtToSamlClaimsAsync(claims) : null);
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(sequenceData.DownPartyLink.Id, claims, keySequenceData.Error, keySequenceData.ErrorDescription);

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
