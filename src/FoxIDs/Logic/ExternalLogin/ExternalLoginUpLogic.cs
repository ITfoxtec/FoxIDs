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
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly ExternalUserLogic externalUserLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly HrdLogic hrdLogic;

        public ExternalLoginUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, SessionLoginUpPartyLogic sessionLogic, ExternalUserLogic externalUserLogic, ClaimTransformLogic claimTransformLogic, PlanUsageLogic planUsageLogic, HrdLogic hrdLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionLogic = sessionLogic;
            this.externalUserLogic = externalUserLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.planUsageLogic = planUsageLogic;
            this.hrdLogic = hrdLogic;
        }

        public async Task<IActionResult> LoginRedirectAsync(UpPartyLink partyLink, ILoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "AuthMethod, External Login redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            planUsageLogic.LogLoginEvent(PartyTypes.ExternalLogin);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new ExternalLoginUpSequenceData(loginRequest)
            {
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                UpPartyProfileName = partyLink.ProfileName
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.ExtLoginController, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LoginResponseAsync(ExternalLoginUpParty extLoginUpParty, ExternalLoginUpSequenceData sequenceData, List<Claim> claims)
        {
            logger.ScopeTrace(() => "AuthMethod, External Login response.");

            try
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

                (var externalUserClaims, var externalUserActionResult, var deleteSequenceData) = await externalUserLogic.HandleUserAsync(extLoginUpParty, sequenceData, claims,
                    (externalUserUpSequenceData) => { },
                    (errorMessage) => throw new EndpointException(errorMessage) { RouteBinding = RouteBinding });
                if (externalUserActionResult != null)
                {
                    if (deleteSequenceData)
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<ExternalLoginUpSequenceData>();
                    }
                    return externalUserActionResult;
                }

                (claims, var authResponseActionResult) = await AuthResponsePostAsync(extLoginUpParty, sequenceData, claims, externalUserClaims);
                if (authResponseActionResult != null)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<ExternalLoginUpSequenceData>();
                    return authResponseActionResult;
                }

                await sequenceLogic.RemoveSequenceDataAsync<ExternalLoginUpSequenceData>();
                return await LoginResponseDownAsync(sequenceData, claims);
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                return await LoginResponseErrorAsync(sequenceData, error: orex.Error, errorDescription: orex.ErrorDescription);
            }
        }

        public async Task<IActionResult> AuthResponsePostAsync(ExternalUserUpSequenceData externalUserSequenceData, IEnumerable<Claim> externalUserClaims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: true);
            var party = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(externalUserSequenceData.UpPartyId);
            
            try
            {
                (var claims, var actionResult) = await AuthResponsePostAsync(party, sequenceData, externalUserSequenceData.Claims?.ToClaimList(), externalUserClaims);
                if (actionResult != null)
                {
                    return actionResult;
                }
                return await LoginResponseDownAsync(sequenceData, claims);
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                return await LoginResponseErrorAsync(sequenceData, error: orex.Error, errorDescription: orex.ErrorDescription);
            }
        }

        private async Task<(List<Claim>, IActionResult actionResult)> AuthResponsePostAsync(ExternalLoginUpParty extLoginUpParty, ExternalLoginUpSequenceData sequenceData, List<Claim> claims, IEnumerable<Claim> externalUserClaims)
        {
            claims = externalUserLogic.AddExternalUserClaims(extLoginUpParty, claims, externalUserClaims);

            (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(extLoginUpParty.ExternalUserLoadedClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
            if (actionResult != null)
            {
                return (null, actionResult);
            }

            await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), sequenceData.UpPartyProfileName, PartyTypes.ExternalLogin);

            logger.ScopeTrace(() => $"AuthMethod, External Login, output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return (transformedClaims, null);
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

        public async Task<IActionResult> LoginResponseErrorAsync(ExternalLoginUpSequenceData sequenceData, LoginSequenceError? loginError = null, string error = null, string errorDescription = null)
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
                    return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyLink.Id, ErrorToOAuth2OidcString(loginError, error), errorDescription);
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, status: ErrorToSamlStatus(loginError, error));

                default:
                    throw new NotSupportedException($"Connection type '{sequenceData.DownPartyLink.Type}' not supported.");
            }
        }

        private string ErrorToOAuth2OidcString(LoginSequenceError? loginError, string error)
        {
            if (!error.IsNullOrWhiteSpace())
            {
                return error;
            }

            switch (loginError)
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

        private Saml2StatusCodes ErrorToSamlStatus(LoginSequenceError? loginError, string error)
        {
            if (!error.IsNullOrWhiteSpace())
            {
                return SamlConvertLogic.ErrorToSamlStatus(error);
            }

            switch (loginError)
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
