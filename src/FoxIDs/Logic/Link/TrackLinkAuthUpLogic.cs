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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkAuthUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ExternalUserLogic externalUserLogic;
        private readonly ClaimValidationLogic claimValidationLogic;

        public TrackLinkAuthUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, PlanUsageLogic planUsageLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, ClaimTransformLogic claimTransformLogic, ExternalUserLogic externalUserLogic, ClaimValidationLogic claimValidationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.planUsageLogic = planUsageLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.externalUserLogic = externalUserLogic;
            this.claimValidationLogic = claimValidationLogic;
        }

        public async Task<IActionResult> AuthRequestAsync(UpPartyLink partyLink, LoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "AuthMethod, Environment Link auth request.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            planUsageLogic.LogLoginEvent(PartyTypes.TrackLink);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(partyId);

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

            return HttpContext.GetTrackDownPartyUrl(party.ToDownTrackName, party.ToDownPartyName, party.SelectedUpParties, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkAuthRequest, includeKeySequence: true).ToRedirectResult(RouteBinding.DisplayName);
        }

        public async Task<IActionResult> AuthResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, Environment Link auth response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToDownTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkDownSequenceData>(keySequence, party.ToDownTrackName);
            if (party.ToDownPartyName != keySequenceData.KeyName)
            {
                throw new Exception($"Incorrect application registration name '{keySequenceData.KeyName}', expected application registration name '{party.ToDownPartyName}'.");
            }

            await sequenceLogic.ValidateAndSetSequenceAsync(keySequenceData.UpPartySequenceString);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>(remove: false);

            List<Claim> claims = keySequenceData.Claims?.ToClaimList();
            if (keySequenceData.Error.IsNullOrEmpty())
            {
                var externalSessionId = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.SessionId);
                externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionIdMax, nameof(externalSessionId), "Session state or claim");
                claims = claims.Where(c => c.Type != JwtClaimTypes.SessionId &&
                    c.Type != Constants.JwtClaimTypes.AuthMethod && c.Type != Constants.JwtClaimTypes.AuthMethodType &&
                    c.Type != Constants.JwtClaimTypes.UpParty && c.Type != Constants.JwtClaimTypes.UpPartyType).ToList();
                claims.AddClaim(Constants.JwtClaimTypes.AuthMethod, party.Name);
                claims.AddClaim(Constants.JwtClaimTypes.AuthMethodType, party.Type.GetPartyTypeValue());
                claims.AddClaim(Constants.JwtClaimTypes.UpParty, party.Name);
                claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, party.Type.GetPartyTypeValue());

                if (party.PipeExternalId)
                {
                    var subject = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject);
                    if (subject.IsNullOrEmpty())
                    {
                        subject = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Email);
                    }
                    if (!subject.IsNullOrEmpty())
                    {
                        claims = claims.Where(c => c.Type != JwtClaimTypes.Subject).ToList();
                        claims.Add(new Claim(JwtClaimTypes.Subject, $"{party.Name}|{subject}"));
                    }
                }

                var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                claims = claimValidationLogic.ValidateUpPartyClaims(party.Claims, transformedClaims);
                logger.ScopeTrace(() => $"AuthMethod, Environment Link transformed JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                (var externalUserActionResult, var externalUserClaims) = await externalUserLogic.HandleUserAsync(party, claims,
                    (externalUserUpSequenceData) =>
                    {
                        externalUserUpSequenceData.ExternalSessionId = externalSessionId;
                        externalUserUpSequenceData.Error = keySequenceData.Error;
                        externalUserUpSequenceData.ErrorDescription = keySequenceData.ErrorDescription;
                    },
                    (errorMessage) => throw new EndpointException(errorMessage) { RouteBinding = RouteBinding });
                if (externalUserActionResult != null)
                {
                    return externalUserActionResult;
                }

                claims = await AuthResponsePostAsync(party, sequenceData, claims, externalUserClaims, externalSessionId);
            }

            await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>();
            return await AuthResponseDownAsync(sequenceData, claims, keySequenceData.Error, keySequenceData.ErrorDescription);
        }

        public async Task<IActionResult> AuthResponsePostAsync(ExternalUserUpSequenceData externalUserSequenceData, IEnumerable<Claim> externalUserClaims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>(remove: true);
            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(externalUserSequenceData.UpPartyId);

            var claims = await AuthResponsePostAsync(party, sequenceData, externalUserSequenceData.Claims?.ToClaimList(), externalUserClaims, externalUserSequenceData.ExternalSessionId);
            return await AuthResponseDownAsync(sequenceData, claims, externalUserSequenceData.Error, externalUserSequenceData.ErrorDescription);
        }

        private async Task<List<Claim>> AuthResponsePostAsync(TrackLinkUpParty party, TrackLinkUpSequenceData sequenceData, List<Claim> claims, IEnumerable<Claim> externalUserClaims, string externalSessionId)
        {
            claims = externalUserLogic.AddExternalUserClaims(party, claims, externalUserClaims);

            var sessionId = await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, party.DisableSingleLogout ? null : sequenceData.DownPartyLink, claims, externalSessionId);
            if (!sessionId.IsNullOrEmpty())
            {
                claims.AddClaim(JwtClaimTypes.SessionId, sessionId);
            }

            if (!sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
            {
                await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), PartyTypes.TrackLink);
            }

            logger.ScopeTrace(() => $"AuthMethod, Environment Link output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return claims;
        }

        private async Task<IActionResult> AuthResponseDownAsync(TrackLinkUpSequenceData sequenceData, List<Claim> claims, string error, string errorDescription)
        {
            switch (sequenceData.DownPartyLink.Type)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    if (error.IsNullOrEmpty())
                    {
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyLink.Id, claims);
                    }
                    else
                    {
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyLink.Id, error, errorDescription);
                    }
                case PartyTypes.Saml2:
                    var claimsLogic = serviceProvider.GetService<ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, SamlConvertLogic.ErrorToSamlStatus(error), claims != null ? await claimsLogic.FromJwtToSamlClaimsAsync(claims) : null);
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(sequenceData.DownPartyLink.Id, claims, error, errorDescription);

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
