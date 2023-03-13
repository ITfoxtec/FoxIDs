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
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly HrdLogic hrdLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ClaimValidationLogic claimValidationLogic;

        public TrackLinkUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, HrdLogic hrdLogic, ClaimTransformLogic claimTransformLogic, ClaimValidationLogic claimValidationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.claimValidationLogic = claimValidationLogic;
        }

        public async Task<IActionResult> LinkRequestAsync(UpPartyLink partyLink, LoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "Up, Track link request.");
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

            return HttpContext.GetTrackDownPartyUrl(party.ToDownTrackName, party.ToDownPartyName, party.SelectedUpParties, Constants.Routes.TrackLinkController, Constants.Endpoints.LinkRequest, includeKeySequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LinkResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, Track link response.");
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

            List<Claim> validClaims = null;
            if (!keySequenceData.Error.IsNullOrEmpty())
            {
                var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), keySequenceData.Claims.ToClaimList());
                validClaims = claimValidationLogic.ValidateUpPartyClaims(party.Claims, transformedClaims);
                logger.ScopeTrace(() => $"Up, Track link output JWT claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            }

            if (!sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
            {
                await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), PartyTypes.Oidc);
            }

            switch (sequenceData.DownPartyLink.Type)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    if (keySequenceData.Error.IsNullOrEmpty())
                    {
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyLink.Id, validClaims);
                    }
                    else
                    {
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyLink.Id, keySequenceData.Error, keySequenceData.ErrorDescription);
                    }
                case PartyTypes.Saml2:
                    var claimsLogic = serviceProvider.GetService<ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, SamlConvertLogic.ErrorToSamlStatus(keySequenceData.Error), validClaims != null ? await claimsLogic.FromJwtToSamlClaimsAsync(validClaims) : null);
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkDownLogic>().LinkResponseAsync(sequenceData.DownPartyLink.Id, validClaims, keySequenceData.Error, keySequenceData.ErrorDescription);

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
