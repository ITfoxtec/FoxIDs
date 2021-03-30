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
using FoxIDs.Models.Session;

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
        private readonly ClaimTransformationsLogic claimTransformationsLogic;
        private readonly SamlClaimsDownLogic samlClaimsDownLogic;
        private readonly ClaimsDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsDownLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;

        public SamlLogoutDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, FormActionLogic formActionLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, ClaimTransformationsLogic claimTransformationsLogic, SamlClaimsDownLogic samlClaimsDownLogic, ClaimsDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsDownLogic, SingleLogoutDownLogic singleLogoutDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.formActionLogic = formActionLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
            this.claimTransformationsLogic = claimTransformationsLogic;
            this.samlClaimsDownLogic = samlClaimsDownLogic;
            this.claimsDownLogic = claimsDownLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
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
                    throw new NotSupportedException($"SAML binding '{party.LogoutBinding.RequestBinding}' not supported.");
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
                    RelayState = binding.RelayState
                });

                var type = RouteBinding.ToUpParties.First().Type;
                logger.ScopeTrace($"Request, Up type '{type}'.");
                switch (type)
                {
                    case PartyTypes.Login:
                        return await serviceProvider.GetService<LogoutUpLogic>().LogoutRedirect(RouteBinding.ToUpParties.First(), GetLogoutRequest(party, saml2LogoutRequest));
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcEndSessionUpLogic<OidcUpParty, OidcUpClient>>().EndSessionRequestRedirectAsync(RouteBinding.ToUpParties.First(), GetLogoutRequest(party, saml2LogoutRequest));
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutRequestRedirectAsync(RouteBinding.ToUpParties.First(), GetSamlLogoutRequest(party, saml2LogoutRequest));

                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
                }
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await LogoutResponseAsync(party, samlConfig, saml2LogoutRequest.Id.Value, binding.RelayState, ex.Status);
            }
        }

        private LogoutRequest GetLogoutRequest(SamlDownParty party, Saml2LogoutRequest saml2LogoutRequest)
        {
            return new LogoutRequest
            {
                DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.SingleLogoutUrl), Id = party.Id, Type = party.Type },
                SessionId = saml2LogoutRequest.SessionIndex,
                RequireLogoutConsent = false,
                PostLogoutRedirect = true,
            };
        }

        private LogoutRequest GetSamlLogoutRequest(SamlDownParty party, Saml2LogoutRequest saml2LogoutRequest)
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
                DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.SingleLogoutUrl), Id = party.Id, Type = party.Type },
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

        public async Task<IActionResult> LogoutResponseAsync(string partyId, Saml2StatusCodes status = Saml2StatusCodes.Success, string sessionIndex = null)
        {
            logger.SetScopeProperty("downPartyId", partyId);

            var party = await tenantRepository.GetAsync<SamlDownParty>(partyId);
            ValidatePartyLogoutSupport(party);

            var samlConfig = saml2ConfigurationLogic.GetSamlDownConfig(party, true);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlDownSequenceData>(false);
            return await LogoutResponseAsync(party, samlConfig, sequenceData.Id, sequenceData.RelayState, status, sessionIndex);
        }

        private Task<IActionResult> LogoutResponseAsync(SamlDownParty party, Saml2Configuration samlConfig, string inResponseTo, string relayState, Saml2StatusCodes status, string sessionIndex = null) 
        {
            logger.ScopeTrace($"Down, SAML Logout response{(status != Saml2StatusCodes.Success ? " error" : string.Empty)}, Status code '{status}'.");

            var binding = party.LogoutBinding.ResponseBinding;
            logger.ScopeTrace($"Binding '{binding}'");
            switch (binding)
            {
                case SamlBindingTypes.Redirect:
                    return LogoutResponseAsync(samlConfig, inResponseTo, relayState, party.LoggedOutUrl, new Saml2RedirectBinding(), status, sessionIndex);
                case SamlBindingTypes.Post:
                    return LogoutResponseAsync(samlConfig, inResponseTo, relayState, party.LoggedOutUrl, new Saml2PostBinding(), status, sessionIndex);
                default:
                    throw new NotSupportedException($"SAML binding '{binding}' not supported.");
            }
        }

        private async Task<IActionResult> LogoutResponseAsync<T>(Saml2Configuration samlConfig, string inResponseTo, string relayState, string loggedOutUrl, Saml2Binding<T> binding, Saml2StatusCodes status, string sessionIndex)
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
            logger.ScopeTrace($"Logged out URL '{loggedOutUrl}'.");
            logger.ScopeTrace("Down, SAML Logout response.", triggerEvent: true);

            await sequenceLogic.RemoveSequenceDataAsync<SamlDownSequenceData>();
            await formActionLogic.RemoveFormActionSequenceDataAsync(loggedOutUrl);
            if (binding is Saml2Binding<Saml2RedirectBinding>)
            {
                return await (binding as Saml2RedirectBinding).ToActionFormResultAsync();
            }
            if (binding is Saml2Binding<Saml2PostBinding>)
            {
                return await (binding as Saml2PostBinding).ToActionFormResultAsync();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public async Task<IActionResult> SingleLogoutRequestAsync(string partyId, SingleLogoutSequenceData sequenceData)
        {
            logger.ScopeTrace("Down, SAML Single Logout request.");
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<SamlDownParty>(partyId);
            if (!ValidatePartySingleLogoutSupport(party))
            {
                return await singleLogoutDownLogic.HandleSingleLogoutAsync(sequenceData);
            }
            
            var claims = await claimsDownLogic.FromJwtToSamlClaimsAsync(sequenceData.Claims.ToClaimList());

            switch (party.LogoutBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await SingleLogoutRequestAsync(party, new Saml2RedirectBinding(), claims);
                case SamlBindingTypes.Post:
                    return await SingleLogoutRequestAsync(party, new Saml2PostBinding(), claims);
                default:
                    throw new NotSupportedException($"SAML binding '{party.LogoutBinding.RequestBinding}' not supported.");
            }
        }

        private bool ValidatePartySingleLogoutSupport(SamlDownParty party)
        {
            if (party.LogoutBinding == null || party.SingleLogoutUrl.IsNullOrEmpty() || party.Keys?.Count <= 0)
            {
                return false;
            }
            return true;
        }

        private async Task<IActionResult> SingleLogoutRequestAsync<T>(SamlDownParty party, Saml2Binding<T> binding, IEnumerable<Claim> claims)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlDownConfig(party, true);

            claims = await claimTransformationsLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);

            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig)
            {
                NameId = samlClaimsDownLogic.GetNameId(claims),
                Destination = new Uri(party.SingleLogoutUrl),
                SessionIndex = samlClaimsDownLogic.GetSessionIndex(claims)
            };

            binding.RelayState = SequenceString;
            binding.Bind(saml2LogoutRequest);
            logger.ScopeTrace($"SAML Single Logout request '{saml2LogoutRequest.XmlDocument.OuterXml}'.");
            logger.ScopeTrace($"Single logged out URL '{party.SingleLogoutUrl}'.");
            logger.ScopeTrace("Down, SAML Single Logout request.", triggerEvent: true);

            await formActionLogic.RemoveFormActionSequenceDataAsync(party.SingleLogoutUrl);
            if (binding is Saml2Binding<Saml2RedirectBinding>)
            {
                return await (binding as Saml2RedirectBinding).ToActionFormResultAsync();
            }
            if (binding is Saml2Binding<Saml2PostBinding>)
            {
                return await (binding as Saml2PostBinding).ToActionFormResultAsync();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public async Task<IActionResult> SingleLogoutResponseAsync(string partyId)
        {
            logger.ScopeTrace("Down, SAML Single Logout response.");
            logger.SetScopeProperty("downPartyId", partyId);
            var party = await tenantRepository.GetAsync<SamlDownParty>(partyId);

            switch (party.LogoutBinding.ResponseBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await SingleLogoutResponseAsync(party, new Saml2RedirectBinding());
                case SamlBindingTypes.Post:
                    return await SingleLogoutResponseAsync(party, new Saml2PostBinding());
                default:
                    throw new NotSupportedException($"SAML binding '{party.LogoutBinding.RequestBinding}' not supported.");
            }
        }

        private async Task<IActionResult> SingleLogoutResponseAsync<T>(SamlDownParty party, Saml2Binding<T> binding)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlDownConfig(party);

            var saml2LogoutResponse = new Saml2LogoutResponse(samlConfig);
            binding.ReadSamlResponse(HttpContext.Request.ToGenericHttpRequest(), saml2LogoutResponse);
            logger.ScopeTrace($"SAML Single Logout response '{saml2LogoutResponse.XmlDocument.OuterXml}'.");
            
            ValidateLogoutResponse(party, saml2LogoutResponse);
            await sequenceLogic.ValidateSequenceAsync(binding.RelayState);

            binding.Unbind(HttpContext.Request.ToGenericHttpRequest(), saml2LogoutResponse);
            logger.ScopeTrace("Down, SAML Single Logout response accepted.", triggerEvent: true);

            return await singleLogoutDownLogic.HandleSingleLogoutAsync();
        }

        private void ValidateLogoutResponse(SamlDownParty party, Saml2LogoutResponse saml2LogoutResponse)
        {
            var requestIssuer = saml2LogoutResponse.Issuer;
            logger.SetScopeProperty("Issuer", requestIssuer);

            if (!party.Issuer.Equals(requestIssuer))
            {
                throw new SamlRequestException($"Invalid issuer '{requestIssuer}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
            }
        }
    }
}
