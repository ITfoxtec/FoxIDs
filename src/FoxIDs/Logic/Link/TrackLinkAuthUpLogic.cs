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
        private readonly AuditLogic auditLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ExtendedUiLogic extendedUiLogic;
        private readonly ExternalUserLogic externalUserLogic;
        private readonly ClaimValidationLogic claimValidationLogic;

        public TrackLinkAuthUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, PlanUsageLogic planUsageLogic, AuditLogic auditLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, ClaimTransformLogic claimTransformLogic, ExtendedUiLogic extendedUiLogic, ExternalUserLogic externalUserLogic, ClaimValidationLogic claimValidationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.planUsageLogic = planUsageLogic;
            this.auditLogic = auditLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.extendedUiLogic = extendedUiLogic;
            this.externalUserLogic = externalUserLogic;
            this.claimValidationLogic = claimValidationLogic;
        }

        public async Task<IActionResult> AuthRequestAsync(UpPartyLink partyLink, ILoginRequest loginRequest, string hrdLoginUpPartyName = null, bool logPlanUsage = true)
        {
            logger.ScopeTrace(() => "AuthMethod, Environment Link auth request.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            logger.SetScopeProperty(Constants.Logs.UpPartyType, PartyTypes.TrackLink.ToString());

            if (logPlanUsage)
            {
                planUsageLogic.LogLoginEvent(PartyTypes.TrackLink);
            }

            await loginRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new TrackLinkUpSequenceData(loginRequest)
            {
                KeyName = partyLink.Name,
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                UpPartyProfileName = partyLink.ProfileName
            }, setKeyValidUntil: true, partyName: party.Name);

            var profile = GetProfile(party, partyLink.ProfileName);

            var selectedUpParties = party.SelectedUpParties;
            if (profile != null && profile.SelectedUpParties?.Count() > 0)
            {
                selectedUpParties = profile.SelectedUpParties;
            }

            return HttpContext.GetTrackDownPartyUrl(party.ToDownTrackName, party.ToDownPartyName, party.SelectedUpParties, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkAuthRequest, includeKeySequence: true).ToRedirectResult();
        }

        private TrackLinkUpPartyProfile GetProfile(TrackLinkUpParty party, string profileName)
        {
            if (!profileName.IsNullOrEmpty() && party.Profiles != null)
            {
                return party.Profiles.Where(p => p.Name == profileName).FirstOrDefault();
            }
            return null;
        }

        public async Task<IActionResult> AuthResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, Environment Link auth response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            logger.SetScopeProperty(Constants.Logs.DownPartyType, PartyTypes.TrackLink.ToString());
            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToDownTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkDownSequenceData>(keySequence, party.ToDownTrackName);
            if (party.ToDownPartyName != keySequenceData.KeyName)
            {
                throw new Exception($"Incorrect application registration name '{keySequenceData.KeyName}', expected application registration name '{party.ToDownPartyName}'.");
            }

            await sequenceLogic.ValidateAndSetSequenceAsync(keySequenceData.UpPartySequenceString);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>(partyName: partyId.PartyIdToName(), remove: false);

            List<Claim> claims = keySequenceData.Claims?.ToClaimList();
            if (!keySequenceData.Error.IsNullOrEmpty())
            {
                await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>(partyName: party.Name);
                return await AuthResponseDownAsync(sequenceData, null, keySequenceData.Error, keySequenceData.ErrorDescription);
            }

            var externalSessionId = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.SessionId);
            externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionIdMax, nameof(externalSessionId), "Session state or claim");
            claims = claims.Where(c => c.Type != JwtClaimTypes.SessionId &&
                c.Type != Constants.JwtClaimTypes.AuthMethod && c.Type != Constants.JwtClaimTypes.AuthProfileMethod && c.Type != Constants.JwtClaimTypes.AuthMethodType &&
                c.Type != Constants.JwtClaimTypes.UpParty && c.Type != Constants.JwtClaimTypes.UpPartyType).ToList();
            claims.AddClaim(Constants.JwtClaimTypes.AuthMethod, party.Name);
            if (!sequenceData.UpPartyProfileName.IsNullOrEmpty())
            {
                claims.AddClaim(Constants.JwtClaimTypes.AuthProfileMethod, sequenceData.UpPartyProfileName);
            }
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

            try
            {
                await sessionUpPartyLogic.CreateOrUpdateMarkerSessionAsync(party, sequenceData.DownPartyLink, externalSessionId);

                (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
                if (actionResult != null)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>(partyName: party.Name);
                    return actionResult;
                }
                claims = claimValidationLogic.ValidateUpPartyClaims(party.Claims, transformedClaims);
                logger.ScopeTrace(() => $"AuthMethod, Environment Link transformed JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var extendedUiActionResult = await HandleExtendedUiAsync(party, sequenceData, claims, externalSessionId);
                if (extendedUiActionResult != null)
                {
                    return extendedUiActionResult;
                }

                (var externalUserClaims, var externalUserActionResult) = await HandleExternalUserAsync(party, sequenceData, claims, externalSessionId);
                if (externalUserActionResult != null)
                {
                    return externalUserActionResult;
                }

                await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>(partyName: party.Name);
                return await AuthResponsePostAsync(party, sequenceData, claims, externalUserClaims, externalSessionId);
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>(partyName: party.Name);
                return await AuthResponseDownAsync(sequenceData, null, orex.Error, orex.ErrorDescription);
            }
        }

        private async Task<IActionResult> HandleExtendedUiAsync(TrackLinkUpParty party, TrackLinkUpSequenceData sequenceData, IEnumerable<Claim> claims, string externalSessionId)
        {
            var extendedUiActionResult = await extendedUiLogic.HandleUiAsync(party, sequenceData, claims,
                (extendedUiUpSequenceData) =>
                {
                    extendedUiUpSequenceData.ExternalSessionId = externalSessionId;
                });

            return extendedUiActionResult;
        }

        private async Task<(IEnumerable<Claim>, IActionResult)> HandleExternalUserAsync(TrackLinkUpParty party, TrackLinkUpSequenceData sequenceData, IEnumerable<Claim> claims, string externalSessionId)
        {
            (var externalUserClaims, var externalUserActionResult, var deleteSequenceData) = await externalUserLogic.HandleUserAsync(party, sequenceData, claims,
                (externalUserUpSequenceData) =>
                {
                    externalUserUpSequenceData.ExternalSessionId = externalSessionId;
                },
                (errorMessage) => throw new EndpointException(errorMessage) { RouteBinding = RouteBinding });
            if (externalUserActionResult != null)
            {
                if (deleteSequenceData)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>(partyName: party.Name);
                }
            }

            return (externalUserClaims, externalUserActionResult);
        }

        public async Task<IActionResult> AuthResponsePostExtendedUiAsync(ExtendedUiUpSequenceData extendedUiSequenceData, IEnumerable<Claim> claims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>(partyName: extendedUiSequenceData.UpPartyId.PartyIdToName(), remove: false);
            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(extendedUiSequenceData.UpPartyId);

            try
            {
                (var externalUserClaims, var externalUserActionResult) = await HandleExternalUserAsync(party, sequenceData, claims, extendedUiSequenceData.ExternalSessionId);
                if (externalUserActionResult != null)
                {
                    return externalUserActionResult;
                }

                await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>(partyName: party.Name);
                return await AuthResponsePostAsync(party, sequenceData, claims, externalUserClaims, extendedUiSequenceData.ExternalSessionId);
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                return await AuthResponseDownAsync(sequenceData, null, orex.Error, orex.ErrorDescription);
            }
        }

        public async Task<IActionResult> AuthResponsePostExternalUserAsync(ExternalUserUpSequenceData externalUserSequenceData, IEnumerable<Claim> externalUserClaims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>(partyName: externalUserSequenceData.UpPartyId.PartyIdToName(), remove: true);
            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(externalUserSequenceData.UpPartyId);

            try
            {
                return await AuthResponsePostAsync(party, sequenceData, externalUserSequenceData.Claims?.ToClaimList(), externalUserClaims, externalUserSequenceData.ExternalSessionId);
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                return await AuthResponseDownAsync(sequenceData, null, orex.Error, orex.ErrorDescription);
            }
        }

        private async Task<IActionResult> AuthResponsePostAsync(TrackLinkUpParty party, TrackLinkUpSequenceData sequenceData, IEnumerable<Claim> claims, IEnumerable<Claim> externalUserClaims, string externalSessionId)
        {
            claims = externalUserLogic.AddExternalUserClaims(party, claims, externalUserClaims);

            (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(party.ExitClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
            if (actionResult != null)
            {
                await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>(partyName: party.Name);
                return actionResult;
            }

            var sessionId = await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, transformedClaims, externalSessionId);
            if (!sessionId.IsNullOrEmpty())
            {
                transformedClaims.AddOrReplaceClaim(JwtClaimTypes.SessionId, sessionId);
            }

            await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), sequenceData.UpPartyProfileName, PartyTypes.TrackLink);

            logger.ScopeTrace(() => $"AuthMethod, Environment Link output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return await AuthResponseDownAsync(sequenceData, transformedClaims);
        }

        private async Task<IActionResult> AuthResponseDownAsync(TrackLinkUpSequenceData sequenceData, List<Claim> claims, string error = null, string errorDescription = null)
        {
            if (error.IsNullOrEmpty() && claims != null)
            {
                auditLogic.LogLoginEvent(PartyTypes.TrackLink, sequenceData.UpPartyId, claims);
            }

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
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, SamlConvertLogic.ErrorToSamlStatus(error), jwtClaims: claims);
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(sequenceData.DownPartyLink.Id, claims, error, errorDescription);

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
