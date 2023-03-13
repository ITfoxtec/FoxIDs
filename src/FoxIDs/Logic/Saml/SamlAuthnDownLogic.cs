using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
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

namespace FoxIDs.Logic
{
    public class SamlAuthnDownLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly SamlClaimsDownLogic samlClaimsDownLogic;
        private readonly ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;

        public SamlAuthnDownLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, ClaimTransformLogic claimTransformLogic, SamlClaimsDownLogic samlClaimsDownLogic, ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.samlClaimsDownLogic = samlClaimsDownLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
        }

        public async Task<IActionResult> AuthnRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, SAML Authn request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<SamlDownParty>(partyId);

            switch (party.AuthnBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await AuthnRequestAsync(party, new Saml2RedirectBinding());
                case SamlBindingTypes.Post:
                    return await AuthnRequestAsync(party, new Saml2PostBinding());
                default:
                    throw new NotSupportedException($"Binding '{party.AuthnBinding.RequestBinding}' not supported.");
            }
        }

        private async Task<IActionResult> AuthnRequestAsync<T>(SamlDownParty party, Saml2Binding<T> binding)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party);
            var request = HttpContext.Request;

            var saml2AuthnRequest = new Saml2AuthnRequest(samlConfig);
            binding.ReadSamlRequest(request.ToGenericHttpRequest(), saml2AuthnRequest);
            logger.ScopeTrace(() => $"SAML Authn request '{saml2AuthnRequest.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);

            try
            {
                ValidateAuthnRequest(party, saml2AuthnRequest);

                try
                {
                    binding.Unbind(request.ToGenericHttpRequest(), saml2AuthnRequest);
                    logger.ScopeTrace(() => "Down, SAML Authn request accepted.", triggerEvent: true);

                }
                catch (Exception ex)
                {
                    var isex = saml2ConfigurationLogic.GetInvalidSignatureValidationCertificateException(samlConfig, ex);
                    if (isex != null)
                    {
                        throw isex;
                    }
                    throw;
                }

                await sequenceLogic.SaveSequenceDataAsync(new SamlDownSequenceData
                {
                    Id = saml2AuthnRequest.Id.Value,
                    RelayState = binding.RelayState,
                    AcsResponseUrl = GetAcsUrl(party, saml2AuthnRequest),
                });

                var toUpParties = RouteBinding.ToUpParties;
                if (toUpParties.Count() == 1)
                {
                    var toUpParty = toUpParties.First();
                    logger.ScopeTrace(() => $"Request, Up type '{toUpParty:Type}'.");
                    switch (toUpParty.Type)
                    {
                        case PartyTypes.Login:
                            return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(toUpParty, GetLoginRequestAsync(party, saml2AuthnRequest));
                        case PartyTypes.OAuth2:
                            throw new NotImplementedException();
                        case PartyTypes.Oidc:
                            return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(toUpParty, GetLoginRequestAsync(party, saml2AuthnRequest));
                        case PartyTypes.Saml2:
                            return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestRedirectAsync(toUpParty, GetLoginRequestAsync(party, saml2AuthnRequest));
                        case PartyTypes.TrackLink:
                            return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthRequestAsync(toUpParty, GetLoginRequestAsync(party, saml2AuthnRequest));

                        default:
                            throw new NotSupportedException($"Party type '{toUpParty.Type}' not supported.");
                    }
                }
                else
                {
                    return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(GetLoginRequestAsync(party, saml2AuthnRequest));
                }
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await AuthnResponseAsync(party, samlConfig, saml2AuthnRequest.Id.Value, binding.RelayState, GetAcsUrl(party, saml2AuthnRequest), ex.Status);
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
            if (saml2AuthnRequest.AssertionConsumerServiceUrl != null && !party.AcsUrls.Any(u => u.Equals(saml2AuthnRequest.AssertionConsumerServiceUrl.OriginalString, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new EndpointException($"Invalid assertion consumer service URL '{saml2AuthnRequest.AssertionConsumerServiceUrl.OriginalString}'.") { RouteBinding = RouteBinding };
            }

            var requestIssuer = saml2AuthnRequest.Issuer;
            logger.SetScopeProperty(Constants.Logs.Issuer, requestIssuer);

            if (!party.Issuer.Equals(requestIssuer))
            {
                throw new SamlRequestException($"Invalid issuer '{requestIssuer}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
            }
        }

        private LoginRequest GetLoginRequestAsync(SamlDownParty party, Saml2AuthnRequest saml2AuthnRequest)
        {
            var loginRequest = new LoginRequest { DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.SingleLogoutUrl), Id = party.Id, Type = party.Type } };

            if(saml2AuthnRequest.ForceAuthn.HasValue && saml2AuthnRequest.ForceAuthn.Value)
            {
                loginRequest.LoginAction = LoginAction.RequireLogin;
            }
            else if(saml2AuthnRequest.IsPassive.HasValue && saml2AuthnRequest.IsPassive.Value)
            {
                loginRequest.LoginAction = LoginAction.ReadSession;
            }
            else
            {
                loginRequest.LoginAction = LoginAction.ReadSessionOrLogin;
            }

            if (!string.IsNullOrWhiteSpace(saml2AuthnRequest.Subject?.NameID?.ID) && saml2AuthnRequest.Subject.NameID.Format == NameIdentifierFormats.Email.OriginalString)
            {
                loginRequest.EmailHint = saml2AuthnRequest.Subject.NameID.ID;
            }

            if (saml2AuthnRequest.RequestedAuthnContext?.AuthnContextClassRef?.Count() > 0)
            {
                loginRequest.Acr = saml2AuthnRequest.RequestedAuthnContext?.AuthnContextClassRef;
            }

            return loginRequest;
        }

        public async Task<IActionResult> AuthnResponseAsync(string partyId, Saml2StatusCodes status = Saml2StatusCodes.Success, IEnumerable<Claim> jwtClaims = null)
        {
            logger.ScopeTrace(() => $"Down, SAML Authn response{(status != Saml2StatusCodes.Success ? " error" : string.Empty )}, Status code '{status}'.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);

            var party = await tenantRepository.GetAsync<SamlDownParty>(partyId);

            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party, includeSigningCertificate: true, includeEncryptionCertificates: true);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlDownSequenceData>(false);
            var claims = jwtClaims != null ? await claimsOAuthDownLogic.FromJwtToSamlClaimsAsync(jwtClaims) : null;
            return await AuthnResponseAsync(party, samlConfig, sequenceData.Id, sequenceData.RelayState, sequenceData.AcsResponseUrl, status, claims);
        }

        private Task<IActionResult> AuthnResponseAsync(SamlDownParty party, Saml2Configuration samlConfig, string inResponseTo, string relayState, string acsUrl, Saml2StatusCodes status, IEnumerable<Claim> claims = null) 
        {
            logger.ScopeTrace(() => $"Down, SAML Authn response{(status != Saml2StatusCodes.Success ? " error" : string.Empty)}, Status code '{status}'.");

            var binding = party.AuthnBinding.ResponseBinding;
            logger.ScopeTrace(() => $"Binding '{binding}'");
            switch (binding)
            {
                case SamlBindingTypes.Redirect:
                    return AuthnResponseAsync( party, samlConfig, inResponseTo, relayState, acsUrl, new Saml2RedirectBinding(), status,claims);
                case SamlBindingTypes.Post:
                    return AuthnResponseAsync(party, samlConfig, inResponseTo, relayState, acsUrl, new Saml2PostBinding(), status, claims);
                default:
                    throw new NotSupportedException($"SAML binding '{binding}' not supported.");
            }
        }

        private async Task<IActionResult> AuthnResponseAsync<T>(SamlDownParty party, Saml2Configuration samlConfig, string inResponseTo, string relayState, string acsUrl, Saml2Binding<T> binding, Saml2StatusCodes status, IEnumerable<Claim> claims)
        {
            binding.RelayState = relayState;

            var saml2AuthnResponse = new FoxIDsSaml2AuthnResponse(settings, samlConfig)
            {
                InResponseTo = new Saml2Id(inResponseTo),
                Status = status,
                Destination = new Uri(acsUrl),
            };
            if (status == Saml2StatusCodes.Success && party != null && claims != null)
            {
                logger.ScopeTrace(() => $"Down, SAML Authn received SAML claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                claims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                logger.ScopeTrace(() => $"Down, SAML Authn output SAML claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                saml2AuthnResponse.SessionIndex = samlClaimsDownLogic.GetSessionIndex(claims);

                saml2AuthnResponse.NameId = samlClaimsDownLogic.GetNameId(claims, party.NameIdFormat);

                var tokenIssueTime = DateTimeOffset.UtcNow;
                var tokenDescriptor = saml2AuthnResponse.CreateTokenDescriptor(samlClaimsDownLogic.GetSubjectClaims(party, claims), party.Issuer, tokenIssueTime, party.IssuedTokenLifetime);

                var authnContext = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.AuthenticationMethod);
                var authenticationInstant = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.AuthenticationInstant);
                var authenticationStatement = saml2AuthnResponse.CreateAuthenticationStatement(authnContext, DateTime.Parse(authenticationInstant));

                var subjectConfirmation = saml2AuthnResponse.CreateSubjectConfirmation(tokenIssueTime, party.SubjectConfirmationLifetime);

                await saml2AuthnResponse.CreateSecurityTokenAsync(tokenDescriptor, authenticationStatement, subjectConfirmation);
            }

            binding.Bind(saml2AuthnResponse);
            var actionResult = await GetAuthnResponseActionResult(binding);
            if (samlConfig.EncryptionCertificate != null)
            {
                // Re-bind to log unencrypted XML.
                saml2AuthnResponse.Config.EncryptionCertificate = null;
                binding.Bind(saml2AuthnResponse);
            }
            logger.ScopeTrace(() => $"SAML Authn response '{saml2AuthnResponse.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => $"ACS URL '{acsUrl}'.");
            logger.ScopeTrace(() => "Down, SAML Authn response.", triggerEvent: true);

            await sequenceLogic.RemoveSequenceDataAsync<SamlDownSequenceData>();
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

        private static async Task<IActionResult> GetAuthnResponseActionResult<T>(Saml2Binding<T> binding)
        {
            if (binding is Saml2Binding<Saml2RedirectBinding>)
            {
                return await (binding as Saml2RedirectBinding).ToActionFormResultAsync();
            }
            else if (binding is Saml2Binding<Saml2PostBinding>)
            {
                return await (binding as Saml2PostBinding).ToActionFormResultAsync();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
