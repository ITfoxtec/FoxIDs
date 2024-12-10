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
using FoxIDs.Logic.Tracks;

namespace FoxIDs.Logic
{
    public class LoginUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceLogic sequenceLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly HrdLogic hrdLogic;

        public LoginUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, PlanUsageLogic planUsageLogic, HrdLogic hrdLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
            this.planUsageLogic = planUsageLogic;
            this.hrdLogic = hrdLogic;
        }

        public async Task<IActionResult> LoginRedirectAsync(UpPartyLink partyLink, LoginRequest loginRequest, bool isAutoRedirect = false, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => $"AuthMethod, Login redirect ({(!isAutoRedirect ? "one" : "auto selected")} authentication method link).");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            planUsageLogic.LogLoginEvent(PartyTypes.Login);

            if (!isAutoRedirect)
            {
                await loginRequest.ValidateObjectAsync();

                await sequenceLogic.SetUiUpPartyIdAsync(partyId);
            }

            await sequenceLogic.SaveSequenceDataAsync(new LoginUpSequenceData
            {
                DownPartyLink = loginRequest.DownPartyLink,
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                ToUpParties = [new HrdUpPartySequenceData { Name = partyLink.Name }],
                LoginAction = loginRequest.LoginAction,
                UserId = loginRequest.UserId,
                MaxAge = loginRequest.MaxAge,
                Email = loginRequest.EmailHint,
                Acr = loginRequest.Acr,
                DoLoginIdentifierStep = loginRequest.EmailHint.IsNullOrWhiteSpace()
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.LoginController, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LoginRedirectAsync(LoginRequest loginRequest)
        {
            logger.ScopeTrace(() => "AuthMethod, Login redirect (multiple authentication method links).");
            (var loginName, var toUpParties) = hrdLogic.GetLoginUpPartyNameAndToUpParties();
            var partyId = await UpParty.IdFormatAsync(RouteBinding, loginName);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await loginRequest.ValidateObjectAsync();

            await sequenceLogic.SetUiUpPartyIdAsync(partyId);

            var hrdUpParties = ToHrdUpPartis(toUpParties);
            var autoSelectedUpParty = await AutoSelectUpPartyAsync(hrdUpParties, loginRequest.EmailHint);
            if (autoSelectedUpParty != null && autoSelectedUpParty.Name != loginName)
            {
                switch (autoSelectedUpParty.Type)
                {
                    case PartyTypes.Login:
                        return await LoginRedirectAsync(autoSelectedUpParty, loginRequest, isAutoRedirect: true, hrdLoginUpPartyName: loginName);
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(autoSelectedUpParty, loginRequest, hrdLoginUpPartyName: loginName);
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestRedirectAsync(autoSelectedUpParty, loginRequest, hrdLoginUpPartyName: loginName);
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthRequestAsync(autoSelectedUpParty, loginRequest, hrdLoginUpPartyName: loginName);
                    case PartyTypes.ExternalLogin:
                        return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginRedirectAsync(autoSelectedUpParty, loginRequest, hrdLoginUpPartyName: loginName);
                    default:
                        throw new NotSupportedException($"Connection type '{autoSelectedUpParty.Type}' not supported.");
                }
            }
            else
            {
                planUsageLogic.LogLoginEvent(PartyTypes.Login);

                await sequenceLogic.SaveSequenceDataAsync(new LoginUpSequenceData
                {
                    DownPartyLink = loginRequest.DownPartyLink,
                    HrdLoginUpPartyName = loginName,
                    UpPartyId = partyId,
                    ToUpParties = hrdUpParties,
                    LoginAction = loginRequest.LoginAction,
                    UserId = loginRequest.UserId,
                    MaxAge = loginRequest.MaxAge,
                    Email = loginRequest.EmailHint,
                    Acr = loginRequest.Acr,
                    DoLoginIdentifierStep = !(autoSelectedUpParty != null && autoSelectedUpParty.Name == loginName && !loginRequest.EmailHint.IsNullOrWhiteSpace())
                });

                return HttpContext.GetUpPartyUrl(loginName, Constants.Routes.LoginController, includeSequence: true).ToRedirectResult();
            }
        }

        public async Task<UpPartyLink> AutoSelectUpPartyAsync(IEnumerable<HrdUpPartySequenceData> toUpParties, string email)
        {
            // Handle up-parties with HRD "*" selection last.
            var toUpPartiesOrdered = toUpParties.OrderBy(u => u.HrdDomains?.Where(h => h == "*").Any() == true);

            // 1) Select specified authentication method
            if (!email.IsNullOrWhiteSpace())
            {
                var emailSplit = email.Split('@');
                if (emailSplit.Count() > 1)
                {
                    var domain = emailSplit[1];
                    var selectedUpParty = toUpPartiesOrdered.Where(up => up.HrdDomains?.Where(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)).Count() > 0).FirstOrDefault();
                    if (selectedUpParty != null)
                    {
                        // A profile is not possible.
                        return new UpPartyLink { Name = selectedUpParty.Name, Type = selectedUpParty.Type };
                    }
                }
            }

            // 2) Select authentication method by HRD
            var hrdUpParties = await hrdLogic.GetHrdSelectionAsync();
            if (hrdUpParties?.Count() > 0)
            {
                var hrdUpParty = hrdUpParties.Where(hu => toUpPartiesOrdered.Any(up => up.Name == hu.SelectedUpPartyName && (hu.SelectedUpPartyProfileName.IsNullOrEmpty() || up.ProfileName == hu.SelectedUpPartyProfileName))).FirstOrDefault();
                if (hrdUpParty != null)
                {
                    return new UpPartyLink { Name = hrdUpParty.SelectedUpPartyName, ProfileName = hrdUpParty.SelectedUpPartyProfileName, Type = hrdUpParty.SelectedUpPartyType };
                }
            }

            // 3) Select authentication method by star
            if (!email.IsNullOrWhiteSpace())
            {
                var starUpParty = toUpPartiesOrdered.Where(up => up.HrdDomains?.Where(d => d == "*").Count() > 0).FirstOrDefault();
                if (starUpParty != null)
                {
                    return new UpPartyLink { Name = starUpParty.Name, ProfileName = starUpParty.ProfileName, Type = starUpParty.Type };
                }
            }

            return null;
        }

        private IEnumerable<HrdUpPartySequenceData> ToHrdUpPartis(IEnumerable<UpPartyLink> toUpParties)
        {
            foreach(var up in toUpParties)
            {
                yield return new HrdUpPartySequenceData
                {
                    Name = up.Name,
                    DisplayName = up.DisplayName,
                    ProfileName = up.ProfileName,
                    ProfileDisplayName = up.ProfileDisplayName,
                    Type = up.Type,
                    HrdDomains = up.HrdDomains,
                    HrdShowButtonWithDomain = up.HrdShowButtonWithDomain,
                    HrdDisplayName = up.HrdDisplayName,
                    HrdLogoUrl = up.HrdLogoUrl
                };
            }
        }

        public async Task<IActionResult> LoginResponseAsync(List<Claim> claims)
        {
            logger.ScopeTrace(() => "AuthMethod, Login response.");

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>();
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            if (!sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
            {
                await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), sequenceData.UpPartyProfileName, PartyTypes.Login);
            }

            logger.ScopeTrace(() => $"Response, Application type {sequenceData.DownPartyLink.Type}.");

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

        public async Task<IActionResult> LoginResponseErrorAsync(LoginUpSequenceData sequenceData, LoginSequenceError error, string errorDescription = null)
        {
            logger.ScopeTrace(() => "Login error response.");

            await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
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
