using FoxIDs.Infrastructure;
using FoxIDs.Logic.Tracks;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ExternalLoginUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly ExternalUserLogic externalUserLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly HrdLogic hrdLogic;

        public ExternalLoginUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, ExternalUserLogic externalUserLogic, PlanUsageLogic planUsageLogic, HrdLogic hrdLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.externalUserLogic = externalUserLogic;
            this.planUsageLogic = planUsageLogic;
            this.hrdLogic = hrdLogic;
        }

        public async Task<IActionResult> LoginRedirectAsync(UpPartyLink partyLink, LoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "AuthMethod, External Login redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            planUsageLogic.LogLoginEvent(PartyTypes.ExternalLogin);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new ExternalLoginUpSequenceData
            {
                DownPartyLink = loginRequest.DownPartyLink,
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                UpPartyProfileName = partyLink.ProfileName,
                LoginAction = loginRequest.LoginAction,
                UserId = loginRequest.UserId,
                MaxAge = loginRequest.MaxAge,
                Email = loginRequest.EmailHint,
                Acr = loginRequest.Acr
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.ExtLoginController, includeSequence: true).ToRedirectResult(RouteBinding.DisplayName);
        }
        public async Task<IActionResult> LoginResponseAsync(ExternalLoginUpParty extLoginUpParty, List<Claim> claims)
        {
            logger.ScopeTrace(() => "AuthMethod, External Login response.");

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            if (!sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
            {
                await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), sequenceData.UpPartyProfileName, PartyTypes.Login);
            }

            (var externalUserActionResult, var externalUserClaims) = await externalUserLogic.HandleUserAsync(extLoginUpParty, claims,
                (externalUserUpSequenceData) => { },
                (errorMessage) => throw new EndpointException(errorMessage) { RouteBinding = RouteBinding });
            if (externalUserActionResult != null)
            {
                return externalUserActionResult;
            }

            claims = await AuthResponsePostAsync(extLoginUpParty, sequenceData, claims, externalUserClaims);
            await sequenceLogic.RemoveSequenceDataAsync<ExternalLoginUpSequenceData>();
            return await LoginResponseDownAsync(sequenceData, claims);
        }

        public async Task<IActionResult> AuthResponsePostAsync(ExternalUserUpSequenceData externalUserSequenceData, IEnumerable<Claim> externalUserClaims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: true);
            var party = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(externalUserSequenceData.UpPartyId);

            var claims = await AuthResponsePostAsync(party, sequenceData, externalUserSequenceData.Claims?.ToClaimList(), externalUserClaims);
            return await LoginResponseDownAsync(sequenceData, claims);
        }

        private async Task<List<Claim>> AuthResponsePostAsync(ExternalLoginUpParty extLoginUpParty, ExternalLoginUpSequenceData sequenceData, List<Claim> claims, IEnumerable<Claim> externalUserClaims)
        {
            claims = externalUserLogic.AddExternalUserClaims(extLoginUpParty, claims, externalUserClaims);

            if (!sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
            {
                await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), sequenceData.UpPartyProfileName, PartyTypes.ExternalLogin);
            }

            logger.ScopeTrace(() => $"AuthMethod, External Login, output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return claims;
        }

        public async Task<IActionResult> LoginResponseDownAsync(ExternalLoginUpSequenceData sequenceData, List<Claim> claims)
        {
            logger.ScopeTrace(() => $"AuthMethod, External Login, Application type {sequenceData.DownPartyLink.Type}.");
            switch (sequenceData.DownPartyLink.Type)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyLink.Id, claims);
                case PartyTypes.Saml2:
                    claims.AddClaim(Constants.JwtClaimTypes.SubFormat, NameIdentifierFormats.Persistent.OriginalString);
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, jwtClaims: claims);
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(sequenceData.DownPartyLink.Id, claims);

                default:
                    throw new NotSupportedException();
            }
        }

        public async Task<IActionResult> LoginResponseErrorAsync(ExternalLoginUpSequenceData sequenceData, LoginSequenceError error, string errorDescription = null)
        {
            logger.ScopeTrace(() => "External Login error response.");

            await sequenceLogic.RemoveSequenceDataAsync<ExternalLoginUpSequenceData>();
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            logger.ScopeTrace(() => $"Response, Application type '{sequenceData.DownPartyLink.Type}'.");
            switch (sequenceData.DownPartyLink.Type)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyLink.Id, ErrorToOAuth2OidcString(error), errorDescription);
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, status: ErrorToSamlStatus(error));

                default:
                    throw new NotSupportedException($"Connection type '{sequenceData.DownPartyLink.Type}' not supported.");
            }
        }

        private string ErrorToOAuth2OidcString(LoginSequenceError error)
        {
            switch (error)
            {
                // Default
                case LoginSequenceError.LoginCanceled:
                    return Constants.OAuth.ResponseErrors.LoginCanceled;

                // OAuth


                // Oidc
                case LoginSequenceError.LoginRequired:
                    return IdentityConstants.ResponseErrors.LoginRequired;
                default:
                    throw new NotImplementedException();
            }
        }

        private Saml2StatusCodes ErrorToSamlStatus(LoginSequenceError error)
        {
            switch (error)
            {
                case LoginSequenceError.LoginCanceled:
                    return Saml2StatusCodes.AuthnFailed;

                case LoginSequenceError.LoginRequired:
                    return Saml2StatusCodes.NoAuthnContext;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
