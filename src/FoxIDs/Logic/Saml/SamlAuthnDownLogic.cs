using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using Saml2Http = ITfoxtec.Identity.Saml2.Http;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Saml2;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Logic;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens.Saml2;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;
using ITfoxtec.Identity;

namespace FoxIDs.Logic
{
    public class SamlAuthnDownLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly SamlClaimsDownLogic samlClaimsDownLogic;
        private readonly ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;

        public SamlAuthnDownLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, ClaimTransformLogic claimTransformLogic, SamlClaimsDownLogic samlClaimsDownLogic, ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.samlClaimsDownLogic = samlClaimsDownLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
        }

        public async Task<IActionResult> AuthnRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, SAML Authn request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<SamlDownParty>(partyId);
            await sequenceLogic.SetDownPartyAsync(partyId, PartyTypes.Saml2);

            var samlHttpRequest = HttpContext.Request.ToGenericHttpRequest(validate: true);
            if (samlHttpRequest.Binding is Saml2RedirectBinding || samlHttpRequest.Binding is Saml2PostBinding)
            {
                logger.ScopeTrace(() => $"Binding, configured '{party.AuthnBinding.RequestBinding}', actual '{samlHttpRequest.Binding.GetType().Name}'");
                return await AuthnRequestAsync(party, samlHttpRequest);
            }
            else
            {
                throw new NotSupportedException($"Binding '{samlHttpRequest.Binding.GetType().Name}' not supported.");
            }
        }

        private async Task<IActionResult> AuthnRequestAsync(SamlDownParty party, Saml2Http.HttpRequest samlHttpRequest)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party);

            var saml2AuthnRequest = new Saml2AuthnRequest(samlConfig);
            samlHttpRequest.Binding.ReadSamlRequest(samlHttpRequest, saml2AuthnRequest);
            logger.ScopeTrace(() => $"SAML Authn request '{saml2AuthnRequest.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);

            SamlDownSequenceData sequenceData = null;
            try
            {
                ValidateAuthnRequest(party, saml2AuthnRequest);

                try
                {
                    samlHttpRequest.Binding.Unbind(samlHttpRequest, saml2AuthnRequest);
                    logger.ScopeTrace(() => "AppReg, SAML Authn request accepted.", triggerEvent: true);
                }
                catch (Exception ex)
                {
                    var invalidCertificateException = saml2ConfigurationLogic.GetInvalidSignatureValidationCertificateException(samlConfig, ex);
                    if (invalidCertificateException != null)
                    {
                        throw invalidCertificateException;
                    }
                    else
                    {
                        throw;
                    }
                }

                sequenceData = await sequenceLogic.SaveSequenceDataAsync(new SamlDownSequenceData(GetLoginRequestAsync(party, saml2AuthnRequest))
                {
                    Id = saml2AuthnRequest.Id.Value,
                    RelayState = samlHttpRequest.Binding.RelayState,
                    AcsResponseUrl = GetAcsUrl(party, saml2AuthnRequest),
                });

                (var toUpParties, _) = await serviceProvider.GetService<SessionUpPartyLogic>().GetSessionOrRouteBindingUpParty(RouteBinding.ToUpParties);
                if (toUpParties.Count() == 1)
                {
                    var toUpParty = toUpParties.First();
                    logger.ScopeTrace(() => $"Request, Authentication type '{toUpParty:Type}'.");
                    switch (toUpParty.Type)
                    {
                        case PartyTypes.Login:
                            return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(toUpParty, sequenceData);
                        case PartyTypes.OAuth2:
                            throw new NotImplementedException();
                        case PartyTypes.Oidc:
                            return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(toUpParty, sequenceData);
                        case PartyTypes.Saml2:
                            return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestRedirectAsync(toUpParty, sequenceData);
                        case PartyTypes.TrackLink:
                            return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthRequestAsync(toUpParty, sequenceData);
                        case PartyTypes.ExternalLogin:
                            return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginRedirectAsync(toUpParty, sequenceData);

                        default:
                            throw new NotSupportedException($"Connection type '{toUpParty.Type}' not supported.");
                    }
                }
                else
                {
                    return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(sequenceData);
                }
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await AuthnResponseAsync(party, sequenceData, samlConfig, saml2AuthnRequest.Id.Value, samlHttpRequest.Binding.RelayState, GetAcsUrl(party, saml2AuthnRequest), ex.Status);
            }
        }

        private string GetAcsUrl(SamlDownParty party, Saml2AuthnRequest saml2AuthnRequest)
        {
            if (saml2AuthnRequest.AssertionConsumerServiceUrl != null)
            {
                return saml2AuthnRequest.AssertionConsumerServiceUrl.OriginalString;
            }
            else
            {
                return party.AcsUrls.First();
            }
        }

        private void ValidateAuthnRequest(SamlDownParty party, Saml2AuthnRequest saml2AuthnRequest)
        {
            if (saml2AuthnRequest.AssertionConsumerServiceUrl != null && !party.AcsUrls.Any(u => party.DisableAbsoluteUrls ? saml2AuthnRequest.AssertionConsumerServiceUrl.OriginalString.StartsWith(u, StringComparison.InvariantCultureIgnoreCase) : u.Equals(saml2AuthnRequest.AssertionConsumerServiceUrl.OriginalString, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new EndpointException($"Invalid assertion consumer service URL '{saml2AuthnRequest.AssertionConsumerServiceUrl.OriginalString}' (maybe the request URL do not match the expected relaying party).") { RouteBinding = RouteBinding };
            }

            var requestIssuer = saml2AuthnRequest.Issuer;
            logger.SetScopeProperty(Constants.Logs.Issuer, requestIssuer);

            if (!party.Issuer.Equals(requestIssuer))
            {
                throw new SamlRequestException($"Invalid request issuer '{requestIssuer}', expected issuer '{party.Issuer}' (maybe the request URL do not match the expected relaying party).") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
            }
        }

        private LoginRequest GetLoginRequestAsync(SamlDownParty party, Saml2AuthnRequest saml2AuthnRequest)
        {
            var loginRequest = new LoginRequest { DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.SingleLogoutUrl), Id = party.Id, Type = party.Type } };

            if (saml2AuthnRequest.ForceAuthn.HasValue && saml2AuthnRequest.ForceAuthn.Value)
            {
                loginRequest.LoginAction = LoginAction.SessionUserRequireLogin;
            }
            else if (saml2AuthnRequest.IsPassive.HasValue && saml2AuthnRequest.IsPassive.Value)
            {
                loginRequest.LoginAction = LoginAction.ReadSession;
            }
            else
            {
                loginRequest.LoginAction = LoginAction.ReadSessionOrLogin;
            }

            if (!string.IsNullOrWhiteSpace(saml2AuthnRequest.Subject?.NameID?.ID))
            {
                loginRequest.LoginHint = saml2AuthnRequest.Subject.NameID.ID.Trim().ToLower();
            }

            if (loginRequest.LoginHint.IsNullOrWhiteSpace())
            {
                loginRequest.LoginHint = GetLoginHint();
            }

            if (saml2AuthnRequest.RequestedAuthnContext?.AuthnContextClassRef?.Count() > 0)
            {
                loginRequest.Acr = saml2AuthnRequest.RequestedAuthnContext?.AuthnContextClassRef;
            }

            return loginRequest;
        }

        private string GetLoginHint()
        {
            var loginHintKeys = new[] { "login_hint", "LoginHint", "username" };

            if (HttpContext.Request.Query != null)
            {
                foreach (var key in loginHintKeys)
                {
                    if (HttpContext.Request.Query.TryGetValue(key, out var values))
                    {
                        var loginHintCandidate = values.FirstOrDefault(v => !v.IsNullOrWhiteSpace());
                        if (!loginHintCandidate.IsNullOrWhiteSpace())
                        {
                            return loginHintCandidate.Trim().ToLower();
                        }
                    }
                }
            }

            if (HttpContext.Request.HasFormContentType)
            {
                var form = HttpContext.Request.Form;
                if (form != null)
                {
                    foreach (var key in loginHintKeys)
                    {
                        if (form.TryGetValue(key, out var values))
                        {
                            var loginHintCandidate = values.FirstOrDefault(v => !v.IsNullOrWhiteSpace());
                            if (!loginHintCandidate.IsNullOrWhiteSpace())
                            {
                                return loginHintCandidate.Trim().ToLower();
                            }
                        }
                    }
                }
            }

            return null;
        }

        public async Task<IActionResult> AuthnResponseAsync(string partyId, Saml2StatusCodes status = Saml2StatusCodes.Success, IEnumerable<Claim> jwtClaims = null, IdPInitiatedDownPartyLink idPInitiatedLink = null, bool allowNullSequenceData = false)
        {
            logger.ScopeTrace(() => $"AppReg, SAML Authn response{(status != Saml2StatusCodes.Success ? " error" : string.Empty)}, Status code '{status}'.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);

            var party = await tenantDataRepository.GetAsync<SamlDownParty>(partyId);

            if (idPInitiatedLink != null && !party.AllowUpParties.Where(p => p.Name == idPInitiatedLink.UpPartyName && p.Type == idPInitiatedLink.UpPartyType).Any())
            {
                throw new Exception($"The authentication method '{idPInitiatedLink.UpPartyName}' ({idPInitiatedLink.UpPartyType}) is not a allowed for the application '{party.Name}' ({party.Type}).");
            }

            var sequenceData = idPInitiatedLink == null ? await sequenceLogic.GetSequenceDataAsync<SamlDownSequenceData>(remove: false, allowNull: allowNullSequenceData) : null;
            if (allowNullSequenceData && sequenceData == null)
            {
                return null;
            }

            var acsResponseUrl = idPInitiatedLink == null ? sequenceData.AcsResponseUrl : GetIdPInitiatedAcsResponseUrl(party, idPInitiatedLink);

            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party, includeSigningCertificate: true, includeEncryptionCertificates: true);
            try
            {
                var claims = jwtClaims != null ? await claimsOAuthDownLogic.FromJwtToSamlClaimsAsync(jwtClaims) : null;
                return await AuthnResponseAsync(party, sequenceData, samlConfig, sequenceData?.Id, sequenceData?.RelayState, acsResponseUrl, status, claims);
            }
            catch (SamlRequestException ex)
            {
                if (status == Saml2StatusCodes.Success)
                {
                    logger.Error(ex);
                    return await AuthnResponseAsync(party, sequenceData, samlConfig, sequenceData?.Id, sequenceData?.RelayState, acsResponseUrl, Saml2StatusCodes.Responder);
                }
                throw;
            }
        }

        private string GetIdPInitiatedAcsResponseUrl(SamlDownParty party, IdPInitiatedDownPartyLink idPInitiatedLink)
        {
            if (!idPInitiatedLink.DownPartyRedirectUrl.IsNullOrEmpty())
            {
                if (!party.AcsUrls.Any(u => party.DisableAbsoluteUrls ? idPInitiatedLink.DownPartyRedirectUrl.StartsWith(u, StringComparison.InvariantCultureIgnoreCase) : u.Equals(idPInitiatedLink.DownPartyRedirectUrl, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new Exception($"Invalid IdP-Initiated login assertion consumer service URL '{idPInitiatedLink.DownPartyRedirectUrl}' (maybe the request URL do not match the expected relaying party).");
                }
                return idPInitiatedLink.DownPartyRedirectUrl;
            }
            else
            {
                return party.AcsUrls.First();
            }
        }

        private Task<IActionResult> AuthnResponseAsync(SamlDownParty party, SamlDownSequenceData sequenceData, Saml2Configuration samlConfig, string inResponseTo, string relayState, string acsUrl, Saml2StatusCodes status, IEnumerable<Claim> claims = null) 
        {
            logger.ScopeTrace(() => $"AppReg, SAML Authn response{(status != Saml2StatusCodes.Success ? " error" : string.Empty)}, Status code '{status}'.");

            var binding = party.AuthnBinding.ResponseBinding;
            logger.ScopeTrace(() => $"Binding '{binding}'");
            switch (binding)
            {
                case SamlBindingTypes.Redirect:
                    return AuthnResponseAsync(party, sequenceData, samlConfig, inResponseTo, relayState, acsUrl, new Saml2RedirectBinding(), status, claims);
                case SamlBindingTypes.Post:
                    return AuthnResponseAsync(party, sequenceData, samlConfig, inResponseTo, relayState, acsUrl, new Saml2PostBinding(), status, claims);
                default:
                    throw new NotSupportedException($"SAML binding '{binding}' not supported.");
            }
        }

        private async Task<IActionResult> AuthnResponseAsync(SamlDownParty party, SamlDownSequenceData sequenceData, Saml2Configuration samlConfig, string inResponseTo, string relayState, string acsUrl, Saml2Binding binding, Saml2StatusCodes status, IEnumerable<Claim> claims)
        {
            binding.RelayState = relayState;

            var saml2AuthnResponse = new FoxIDsSaml2AuthnResponse(settings, samlConfig)
            {
                InResponseTo = !inResponseTo.IsNullOrEmpty() ? new Saml2Id(inResponseTo) : null,
                Status = status,
                Destination = new Uri(acsUrl),
            };
            if (status == Saml2StatusCodes.Success && party != null && claims != null)
            {
                logger.ScopeTrace(() => $"AppReg, SAML Authn received SAML claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                (claims, var actionResultTransform) = await claimTransformLogic.TransformAsync(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
                if (actionResultTransform != null)
                {
                    if (sequenceData != null)
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<SamlDownSequenceData>();
                    }
                    return actionResultTransform;
                }
                logger.ScopeTrace(() => $"AppReg, SAML Authn output SAML claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                saml2AuthnResponse.SessionIndex = samlClaimsDownLogic.GetSessionIndex(claims);

                saml2AuthnResponse.NameId = samlClaimsDownLogic.GetNameId(claims, party.NameIdFormat);

                var tokenIssueTime = DateTimeOffset.UtcNow;
                var tokenDescriptor = saml2AuthnResponse.CreateTokenDescriptor(samlClaimsDownLogic.GetSubjectClaims(party, claims), party.Issuer, tokenIssueTime, party.IssuedTokenLifetime);

                var authnContext = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.AuthenticationMethod);
                if (string.IsNullOrEmpty(authnContext))
                {
                    throw new InvalidOperationException($"The authentication method '{ClaimTypes.AuthenticationMethod}' claim is empty.");
                }
                var authenticationInstant = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.AuthenticationInstant);
                if (string.IsNullOrEmpty(authenticationInstant))
                {
                    throw new InvalidOperationException($"The authentication instant '{ClaimTypes.AuthenticationInstant}' claim is empty.");
                }
                var authenticationStatement = saml2AuthnResponse.CreateAuthenticationStatement(authnContext, DateTime.Parse(authenticationInstant));

                var subjectConfirmation = saml2AuthnResponse.CreateSubjectConfirmation(tokenIssueTime, party.SubjectConfirmationLifetime);

                await saml2AuthnResponse.CreateSecurityTokenAsync(tokenDescriptor, authenticationStatement, subjectConfirmation);
            }

            if (sequenceData != null)
            {
                await sequenceLogic.RemoveSequenceDataAsync<SamlDownSequenceData>();
            }
            binding.Bind(saml2AuthnResponse);
            var actionResult = binding.ToSamlActionResult();
            if (samlConfig.EncryptionCertificate != null)
            {
                // Re-bind to log unencrypted XML.
                saml2AuthnResponse.Config.EncryptionCertificate = null;
                binding.Bind(saml2AuthnResponse);
            }
            logger.ScopeTrace(() => $"SAML Authn response '{saml2AuthnResponse.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => $"ACS URL '{acsUrl}'.");
            logger.ScopeTrace(() => "AppReg, SAML Authn response.", triggerEvent: true);

            if (party.RestrictFormAction)
            {
                securityHeaderLogic.AddFormAction(acsUrl);
            }
            else
            {
                securityHeaderLogic.AddFormActionAllowAll();
            }
            return actionResult;
        }
    }
}
