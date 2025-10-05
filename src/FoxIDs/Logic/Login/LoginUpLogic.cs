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
using System.Text.RegularExpressions;
using NetTools;
using System.Net;
using FoxIDs.Repository;

namespace FoxIDs.Logic
{
    public class LoginUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ExtendedUiLogic extendedUiLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly AuditLogic auditLogic;
        private readonly HrdLogic hrdLogic;

        public LoginUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, ClaimTransformLogic claimTransformLogic, ExtendedUiLogic extendedUiLogic, PlanUsageLogic planUsageLogic, AuditLogic auditLogic, HrdLogic hrdLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.extendedUiLogic = extendedUiLogic;
            this.planUsageLogic = planUsageLogic;
            this.auditLogic = auditLogic;
            this.hrdLogic = hrdLogic;
        }

        public async Task<IActionResult> LoginRedirectAsync(UpPartyLink partyLink, ILoginRequest loginRequest, bool isAutoRedirect = false, string hrdLoginUpPartyName = null)
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

            await sequenceLogic.SaveSequenceDataAsync(new LoginUpSequenceData(loginRequest)
            {
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                ToUpParties = [new HrdUpPartySequenceData { Name = partyLink.Name }],
                DoLoginIdentifierStep = loginRequest.LoginHint.IsNullOrWhiteSpace()
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.LoginController, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LoginRedirectAsync(ILoginRequest loginRequest)
        {
            logger.ScopeTrace(() => "AuthMethod, Login redirect (multiple authentication method links).");
            (var loginName, var toUpParties) = hrdLogic.GetLoginUpPartyNameAndToUpParties();
            var partyId = await UpParty.IdFormatAsync(RouteBinding, loginName);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await loginRequest.ValidateObjectAsync();

            await sequenceLogic.SetUiUpPartyIdAsync(partyId);

            var hrdUpParties = ToHrdUpPartis(toUpParties);
            var autoSelectedUpParty = await AutoSelectUpPartyAsync(hrdUpParties, loginRequest.LoginHint);
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
                    LoginHint = loginRequest.LoginHint,
                    Acr = loginRequest.Acr,
                    DoLoginIdentifierStep = !(autoSelectedUpParty != null && autoSelectedUpParty.Name == loginName && !loginRequest.LoginHint.IsNullOrWhiteSpace())
                });

                return HttpContext.GetUpPartyUrl(loginName, Constants.Routes.LoginController, includeSequence: true).ToRedirectResult();
            }
        }

        public async Task<UpPartyLink> AutoSelectUpPartyAsync(IEnumerable<HrdUpPartySequenceData> toUpParties, string userIdentifier)
        {
            // Handle up-parties with HRD "*" selection last.
            var toUpPartiesDomainOrdered = toUpParties.OrderBy(u => u.HrdDomains?.Where(h => h == "*").Any() == true);

            // 1) Select specified authentication method IP address
            if (HttpContext.Connection?.RemoteIpAddress != null)
            {
                foreach (var up in toUpParties)
                {
                    if (up.HrdIPAddressesAndRanges != null && up.HrdIPAddressesAndRanges.Any())
                    {
                        foreach (var ipar in up.HrdIPAddressesAndRanges)
                        {
                            if (ipar.Contains('-') || ipar.Contains('/'))
                            {
                                if(IPAddressRange.Parse(ipar).Contains(HttpContext.Connection.RemoteIpAddress))
                                {
                                    // A profile is not possible.
                                    return new UpPartyLink { Name = up.Name, Type = up.Type };
                                }
                            }
                            else
                            {
                                if (IPAddress.Parse(ipar).Equals(HttpContext.Connection.RemoteIpAddress))
                                {
                                    // A profile is not possible.
                                    return new UpPartyLink { Name = up.Name, Type = up.Type };
                                }
                            }
                        }
                    }
                } 
            }

            // 2) Select specified authentication method by domain
            if (!userIdentifier.IsNullOrWhiteSpace() && userIdentifier.Contains('@'))
            {
                var emailSplit = userIdentifier.Split('@');
                if (emailSplit.Count() > 1)
                {
                    var domain = emailSplit[1];
                    var selectedUpParty = toUpPartiesDomainOrdered.Where(up => up.HrdDomains?.Where(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)).Count() > 0).FirstOrDefault();
                    if (selectedUpParty != null)
                    {
                        // A profile is not possible.
                        return new UpPartyLink { Name = selectedUpParty.Name, Type = selectedUpParty.Type };
                    }
                }
            }

            // 3) Select specified authentication method by regex
            if (!userIdentifier.IsNullOrWhiteSpace())
            {
                foreach (var up in toUpParties)
                {
                    if (up.HrdRegularExpressions != null && up.HrdRegularExpressions.Any())
                    {
                        foreach (var rx in up.HrdRegularExpressions)
                        {
                            var regex = new Regex(rx, RegexOptions.IgnoreCase);
                            if (regex.Match(userIdentifier).Success)
                            {
                                // A profile is not possible.
                                return new UpPartyLink { Name = up.Name, Type = up.Type };
                            }
                        }
                    }
                }
            }

            // 4) Select authentication method by HRD
            var hrdUpParties = await hrdLogic.GetHrdSelectionAsync();
            if (hrdUpParties?.Count() > 0)
            {
                var hrdUpParty = hrdUpParties.Where(hu => toUpPartiesDomainOrdered.Any(up => up.Name == hu.SelectedUpPartyName && (hu.SelectedUpPartyProfileName.IsNullOrEmpty() || up.ProfileName == hu.SelectedUpPartyProfileName))).FirstOrDefault();
                if (hrdUpParty != null)
                {
                    return new UpPartyLink { Name = hrdUpParty.SelectedUpPartyName, ProfileName = hrdUpParty.SelectedUpPartyProfileName, Type = hrdUpParty.SelectedUpPartyType };
                }
            }

            // 5) Select authentication method by star
            if (!userIdentifier.IsNullOrWhiteSpace() && userIdentifier.Contains('@'))
            {
                var starUpParty = toUpParties.Where(up => up.HrdDomains?.Where(d => d == "*").Any() == true).FirstOrDefault();
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
                    HrdIPAddressesAndRanges = up.HrdIPAddressesAndRanges,
                    HrdDomains = up.HrdDomains,
                    HrdRegularExpressions = up.HrdRegularExpressions,
                    HrdAlwaysShowButton = up.HrdAlwaysShowButton,
                    HrdDisplayName = up.HrdDisplayName,
                    HrdLogoUrl = up.HrdLogoUrl
                };
            }
        }

        public async Task<IActionResult> LoginResponseAsync(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData, List<Claim> claims)
        {
            logger.ScopeTrace(() => "AuthMethod, Login response.");

            try
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

                var extendedUiActionResult = await HandleExtendedUiAsync(loginUpParty, sequenceData, claims);
                if (extendedUiActionResult != null)
                {
                    return extendedUiActionResult;
                }

                await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
                return await LoginResponsePostAsync(loginUpParty, sequenceData, claims);
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                return await LoginResponseErrorAsync(sequenceData, error: orex.Error, errorDescription: orex.ErrorDescription);
            }
        }

        private async Task<IActionResult> HandleExtendedUiAsync(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData, IEnumerable<Claim> claims)
        {
            var extendedUiActionResult = await extendedUiLogic.HandleUiAsync(loginUpParty, sequenceData, claims,
                (extendedUiUpSequenceData) => { });

            return extendedUiActionResult;
        }

        public async Task<IActionResult> LoginResponsePostExtendedUiAsync(ExtendedUiUpSequenceData extendedUiSequenceData, IEnumerable<Claim> claims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: true);
            var party = await tenantDataRepository.GetAsync<LoginUpParty>(extendedUiSequenceData.UpPartyId);

            try
            {
                return await LoginResponsePostAsync(party, sequenceData, claims);
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                return await serviceProvider.GetService<LoginUpLogic>().LoginResponseErrorAsync(sequenceData, error: orex.Error, errorDescription: orex.ErrorDescription);
            }
        }

        private async Task<IActionResult> LoginResponsePostAsync(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData, IEnumerable<Claim> claims)
        {
            (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(loginUpParty.ExitClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
            if (actionResult != null)
            {
                await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
                return actionResult;
            }

            await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), sequenceData.UpPartyProfileName, PartyTypes.Login);

            logger.ScopeTrace(() => $"AuthMethod, output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return await LoginResponseDownAsync(sequenceData, transformedClaims);
        }

        private async Task<IActionResult> LoginResponseDownAsync(LoginUpSequenceData sequenceData, List<Claim> claims)
        {
            logger.ScopeTrace(() => $"AuthMethod, Response, Application type {sequenceData.DownPartyLink.Type}.");

            auditLogic.LogLoginEvent(PartyTypes.Login, sequenceData.UpPartyId, claims);

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

        public async Task<IActionResult> LoginResponseErrorAsync(LoginUpSequenceData sequenceData, LoginSequenceError? loginError = null, string error = null, string errorDescription = null)
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
