using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using ITfoxtec.Identity.Saml2.Claims;
using System.Linq;
using FoxIDs.Models.Logic;
using Microsoft.IdentityModel.Tokens.Saml2;
using FoxIDs.Models.Sequences;

namespace FoxIDs.Logic
{
    public class SamlLogoutDownLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly FormActionLogic formActionLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;

        public SamlLogoutDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, FormActionLogic formActionLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.formActionLogic = formActionLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
        }

        public async Task<IActionResult> LogoutRequestAsync(string partyId)
        {
            logger.ScopeTrace("Down, SAML Logout request.");
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<SamlDownParty>(partyId);
            ValidatePartyLogoutSupport(party);

            switch (party.LogoutBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await LogoutRequestAsync(party, new Saml2RedirectBinding());
                case SamlBindingTypes.Post:
                    return await LogoutRequestAsync(party, new Saml2PostBinding());
                default:
                    throw new NotSupportedException($"Binding '{party.LogoutBinding.RequestBinding}' not supported.");
            }
        }

        private void ValidatePartyLogoutSupport(SamlDownParty party)
        {
            if (party.LogoutBinding == null || party.LoggedOutUrl.IsNullOrEmpty() || party.Keys?.Count <= 0)
            {
                throw new EndpointException("Logout not configured.") { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LogoutRequestAsync<T>(SamlDownParty party, Saml2Binding<T> binding)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlDownConfig(party);

            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig);
            binding.ReadSamlRequest(HttpContext.Request.ToGenericHttpRequest(), saml2LogoutRequest);
            logger.ScopeTrace($"SAML Logout request '{saml2LogoutRequest.XmlDocument.OuterXml}'.");

            try
            {
                ValidateLogoutRequest(party, saml2LogoutRequest);
                binding.Unbind(HttpContext.Request.ToGenericHttpRequest(), saml2LogoutRequest);
                logger.ScopeTrace("Down, SAML Logout request accepted.", triggerEvent: true);

                await sequenceLogic.SaveSequenceDataAsync(new SamlDownSequenceData
                {
                    Id = saml2LogoutRequest.Id.Value,
                    RelayState = binding.RelayState,
                    ResponseUrl = party.LoggedOutUrl,
                });
                await formActionLogic.CreateFormActionByUrlAsync(party.LoggedOutUrl);

                var type = RouteBinding.ToUpParties.First().Type;
                logger.ScopeTrace($"Request, Up type '{type}'.");
                switch (type)
                {
                    case PartyTypes.Login:
                        return await serviceProvider.GetService<LogoutUpLogic>().LogoutRedirect(RouteBinding.ToUpParties.First(), new LogoutRequest
                        {
                            DownParty = party,
                            SessionId = saml2LogoutRequest.SessionIndex,
                            RequireLogoutConsent = false,
                            PostLogoutRedirect = true,
                        });
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        throw new NotImplementedException();
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutAsync(RouteBinding.ToUpParties.First(), GetSamlUpLogoutRequest(saml2LogoutRequest, party));

                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
                }
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await LogoutResponseAsync(party.Id, samlConfig, saml2LogoutRequest.Id.Value, binding.RelayState, party.LoggedOutUrl, party.AuthnBinding.ResponseBinding, ex.Status);
            }
        }

        private static LogoutRequest GetSamlUpLogoutRequest(Saml2LogoutRequest saml2LogoutRequest, Party party)
        {
            var samlClaims = new List<Claim>();
            if (saml2LogoutRequest.NameId != null)
            {
                samlClaims.AddClaim(Saml2ClaimTypes.NameId, saml2LogoutRequest.NameId.Value);

                if (saml2LogoutRequest.NameId.Format != null)
                {
                    samlClaims.AddClaim(Saml2ClaimTypes.NameIdFormat, saml2LogoutRequest.NameId.Format.OriginalString);
                }
            }
            return new LogoutRequest
            {
                DownParty = party,
                SessionId = saml2LogoutRequest.SessionIndex,
                RequireLogoutConsent = false,
                PostLogoutRedirect = true,
                Claims = samlClaims,
            };
        }

        private void ValidateLogoutRequest(SamlDownParty party, Saml2LogoutRequest saml2LogoutRequest)
        {
            var requestIssuer = saml2LogoutRequest.Issuer;
            logger.SetScopeProperty("Issuer", requestIssuer);

            if (!party.Issuer.Equals(requestIssuer))
            {
                throw new SamlRequestException($"Invalid issuer '{requestIssuer}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
            }
        }

        private LoginRequest GetLoginRequestAsync(Saml2AuthnRequest saml2AuthnRequest)
        {
            var loginRequest = new LoginRequest();

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

            return loginRequest;
        }

        public async Task<IActionResult> LogoutResponseAsync(string partyId, Saml2StatusCodes status = Saml2StatusCodes.Success, string sessionIndex = null)
        {
            logger.ScopeTrace($"Down, SAML Logout response{(status != Saml2StatusCodes.Success ? " error" : string.Empty )}, Status code '{status}'.");
            logger.SetScopeProperty("downPartyId", partyId);

            var party = await tenantRepository.GetAsync<SamlDownParty>(partyId);
            ValidatePartyLogoutSupport(party);

            var samlConfig = saml2ConfigurationLogic.GetSamlDownConfig(party, true);

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlDownSequenceData>(false);

            logger.ScopeTrace($"Binding '{party.LogoutBinding.ResponseBinding}'");
            switch (party.LogoutBinding.ResponseBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await LogoutResponseAsync(samlConfig, sequenceData.Id, sequenceData.RelayState, sequenceData.ResponseUrl, new Saml2RedirectBinding(), status, sessionIndex);
                case SamlBindingTypes.Post:
                    return await LogoutResponseAsync(samlConfig, sequenceData.Id, sequenceData.RelayState, sequenceData.ResponseUrl, new Saml2PostBinding(), status, sessionIndex);
                default:
                    throw new NotSupportedException($"SAML binding '{party.LogoutBinding.ResponseBinding}' not supported.");
            }            
        }

        private Task<IActionResult> LogoutResponseAsync(string partyId, Saml2Configuration samlConfig, string inResponseTo, string relayState, string loggedOutUrl, SamlBindingTypes binding, Saml2StatusCodes status) 
        {
            logger.ScopeTrace($"Logout response{(status != Saml2StatusCodes.Success ? " error" : string.Empty)}, Status code '{status}'.");
            logger.SetScopeProperty("downPartyId", partyId);

            logger.ScopeTrace($"Binding '{binding}'");
            switch (binding)
            {
                case SamlBindingTypes.Redirect:
                    return LogoutResponseAsync(samlConfig, inResponseTo, relayState, loggedOutUrl, new Saml2RedirectBinding(), status);
                case SamlBindingTypes.Post:
                    return LogoutResponseAsync(samlConfig, inResponseTo, relayState, loggedOutUrl, new Saml2PostBinding(), status);
                default:
                    throw new NotSupportedException($"SAML binding '{binding}' not supported.");
            }
        }

        private async Task<IActionResult> LogoutResponseAsync<T>(Saml2Configuration samlConfig, string inResponseTo, string relayState, string loggedOutUrl, Saml2Binding<T> binding, Saml2StatusCodes status, string sessionIndex = null)
        {
            binding.RelayState = relayState;

            var saml2LogoutResponse = new Saml2LogoutResponse(samlConfig)
            {
                InResponseTo = new Saml2Id(inResponseTo),
                Status = status,
                Destination = new Uri(loggedOutUrl),
                SessionIndex = sessionIndex
            };

            binding.Bind(saml2LogoutResponse);
            logger.ScopeTrace($"SAML Logout response '{saml2LogoutResponse.XmlDocument.OuterXml}'.");
            logger.ScopeTrace($"Logged out url '{loggedOutUrl}'.");
            logger.ScopeTrace("Down, SAML Logout response.", triggerEvent: true);

            await sequenceLogic.RemoveSequenceDataAsync<SamlDownSequenceData>();
            await formActionLogic.RemoveFormActionSequenceDataAsync();
            if (binding is Saml2Binding<Saml2RedirectBinding>)
            {
                return await Task.FromResult((binding as Saml2RedirectBinding).ToActionResult());
            }
            if (binding is Saml2Binding<Saml2PostBinding>)
            {
                return await Task.FromResult((binding as Saml2PostBinding).ToActionResult());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
