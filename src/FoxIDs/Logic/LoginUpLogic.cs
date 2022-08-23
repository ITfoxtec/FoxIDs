using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ITfoxtec.Identity.Saml2.Schemas;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using System.Linq;

namespace FoxIDs.Logic
{
    public class LoginUpLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceLogic sequenceLogic;

        public LoginUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
        }

        public async Task<IActionResult> LoginRedirectAsync(UpPartyLink partyLink, LoginRequest loginRequest, bool isAutoRedirect = false)
        {
            logger.ScopeTrace(() => $"Up, Login redirect ({(!isAutoRedirect ? "one" : "auto selected")} up-party link).");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            if (!isAutoRedirect)
            {
                await loginRequest.ValidateObjectAsync();

                await sequenceLogic.SetUiUpPartyIdAsync(partyId);
            }

            var doIdentifier = loginRequest.EmailHint.IsNullOrWhiteSpace();

            await sequenceLogic.SaveSequenceDataAsync(new LoginUpSequenceData
            {
                DownPartyLink = loginRequest.DownPartyLink,
                UpPartyId = partyId,
                ToUpParties = doIdentifier ? GetToUpPartis(RouteBinding) : null,
                LoginAction = loginRequest.LoginAction,
                UserId = loginRequest.UserId,
                MaxAge = loginRequest.MaxAge,
                Email = loginRequest.EmailHint,
                Acr = loginRequest.Acr,
                DoLoginIdentifierStep = doIdentifier
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.LoginController, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LoginRedirectAsync(List<UpPartyLink> toUpParties, LoginRequest loginRequest)
        {
            logger.ScopeTrace(() => "Up, Login redirect (multiple up-party links).");
            var login = toUpParties.Where(up => up.Type == PartyTypes.Login).FirstOrDefault();
            var loginName = login != null ? login.Name : Constants.DefaultLogin.Name;
            var partyId = await UpParty.IdFormatAsync(RouteBinding, loginName);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await loginRequest.ValidateObjectAsync();

            await sequenceLogic.SetUiUpPartyIdAsync(partyId);

            var autoSelectedUpParty = AutoSelectUpParty(toUpParties, loginRequest.EmailHint);
            if (autoSelectedUpParty != null)
            {
                switch (autoSelectedUpParty.Type)
                {
                    case PartyTypes.Login:
                        return await LoginRedirectAsync(autoSelectedUpParty, loginRequest, isAutoRedirect: true);
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:                        
                        return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(autoSelectedUpParty, loginRequest);
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestRedirectAsync(autoSelectedUpParty, loginRequest);
                    default:
                        throw new NotSupportedException($"Party type '{autoSelectedUpParty.Type}' not supported.");
                }
            }
            else
            {
                await sequenceLogic.SaveSequenceDataAsync(new LoginUpSequenceData
                {
                    DownPartyLink = loginRequest.DownPartyLink,
                    UpPartyId = partyId,
                    ToUpParties = GetToUpPartis(RouteBinding),
                    LoginAction = loginRequest.LoginAction,
                    UserId = loginRequest.UserId,
                    MaxAge = loginRequest.MaxAge,
                    Email = loginRequest.EmailHint,
                    Acr = loginRequest.Acr,
                    DoLoginIdentifierStep = true
                });

                return HttpContext.GetUpPartyUrl(loginName, Constants.Routes.LoginController, includeSequence: true).ToRedirectResult();
            }
        }

        private UpPartyLink AutoSelectUpParty(List<UpPartyLink> toUpParties, string email)
        {
            var starUpParty = toUpParties.Where(up => up.HrdDomains?.Where(d => d == "*").Count() > 0).FirstOrDefault();
            if (starUpParty != null)
            {
                return starUpParty;
            }

            if (!email.IsNullOrWhiteSpace())
            {
                var emailSplit = email.Split('@');
                if (emailSplit.Count() > 1)
                {
                    var domain = emailSplit[1];
                    var selectedUpParty = toUpParties.Where(up => up.HrdDomains?.Where(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)).Count() > 0).FirstOrDefault();
                    if (selectedUpParty != null)
                    {
                        return selectedUpParty;
                    }
                }
            }
            return null;
        }

        private IEnumerable<HrdUpParty> GetToUpPartis(RouteBinding routeBinding)
        {
            return routeBinding.ToUpParties.Select(up => new HrdUpParty { Name = up.Name, Type = up.Type, HrdDomains = up.HrdDomains, HrdDisplayName = up.HrdDisplayName, HrdLogoUrl = up.HrdLogoUrl });
        }

        public async Task<IActionResult> LoginResponseAsync(List<Claim> claims)
        {
            logger.ScopeTrace(() => "Up, Login response.");

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>();
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            logger.ScopeTrace(() => $"Response, Down type {sequenceData.DownPartyLink.Type}.");
            switch (sequenceData.DownPartyLink.Type)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyLink.Id, claims);
                case PartyTypes.Saml2:
                    claims.AddClaim(Constants.JwtClaimTypes.SubFormat, NameIdentifierFormats.Persistent.OriginalString);
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, jwtClaims: claims);

                default:
                    throw new NotSupportedException();
            }
        }

        public async Task<IActionResult> LoginResponseErrorAsync(LoginUpSequenceData sequenceData, LoginSequenceError error, string errorDescription = null)
        {
            logger.ScopeTrace(() => "Login error response.");

            await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            logger.ScopeTrace(() => $"Response, Down type '{sequenceData.DownPartyLink.Type}'.");
            switch (sequenceData.DownPartyLink.Type)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyLink.Id, ErrorToOAuth2OidcString(error), errorDescription);
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, status: ErrorToSamlStatus(error));

                default:
                    throw new NotSupportedException($"Party type '{sequenceData.DownPartyLink.Type}' not supported.");
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
