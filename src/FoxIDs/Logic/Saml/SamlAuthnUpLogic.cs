using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
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
using FoxIDs.Models.Sequences;

namespace FoxIDs.Logic
{
    public class SamlAuthnUpLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;

        public SamlAuthnUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
        }

        public async Task<IActionResult> AuthnRequestAsync(string partyId, LoginRequest loginRequest)
        {
            logger.ScopeTrace("Up, SAML Authn request.");
            logger.SetScopeProperty("upPartyId", partyId);

            await loginRequest.ValidateObjectAsync();

            await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
            {
                DownPartyId = loginRequest.DownParty.Id,
                DownPartyType = loginRequest.DownParty.Type.ToString(),
            });

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);

            var binding = party.AuthnBinding.RequestBinding.ToEnum<SamlBindingType>();
            switch (binding)
            {
                case SamlBindingType.Redirect:
                    return await AuthnRequestAsync(party, new Saml2RedirectBinding(), loginRequest);
                case SamlBindingType.Post:
                    return await AuthnRequestAsync(party, new Saml2PostBinding(), loginRequest);
                default:
                    throw new NotSupportedException($"Binding '{binding}' not supported.");
            }
        }

        private async Task<IActionResult> AuthnRequestAsync<T>(SamlUpParty party, Saml2Binding<T> binding, LoginRequest loginRequest)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party);

            binding.RelayState = SequenceString;
            var saml2AuthnRequest = new Saml2AuthnRequest(samlConfig);

            switch (loginRequest.LoginAction)
            {
                case LoginAction.ReadSession:
                    saml2AuthnRequest.IsPassive = true;
                    break;
                case LoginAction.RequereLogin:
                    saml2AuthnRequest.ForceAuthn = true;
                    break;
                default:
                    break;
            }

            binding.Bind(saml2AuthnRequest);
            logger.ScopeTrace($"SAML Authn request '{saml2AuthnRequest.XmlDocument.OuterXml}'.");
            logger.ScopeTrace($"Authn url '{samlConfig.SingleSignOnDestination?.OriginalString}'.");
            logger.ScopeTrace("Up, Sending SAML Authn request.", triggerEvent: true);

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

        public async Task<IActionResult> AuthnResponseAsync(string partyId)
        {
            logger.ScopeTrace($"Up, SAML Authn response.");
            logger.SetScopeProperty("upPartyId", partyId);

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);

            var binding = party.AuthnBinding.ResponseBinding.ToEnum<SamlBindingType>();
            logger.ScopeTrace($"Binding '{binding}'");
            switch (binding)
            {
                case SamlBindingType.Redirect:
                    return await AuthnResponseAsync(party, new Saml2RedirectBinding());
                case SamlBindingType.Post:
                    return await AuthnResponseAsync(party, new Saml2PostBinding());
                default:
                    throw new NotSupportedException($"SAML binding '{binding}' not supported.");
            }            
        }

        private async Task<IActionResult> AuthnResponseAsync<T>(SamlUpParty party, Saml2Binding<T> binding)
        {
            var request = HttpContext.Request;
            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party);

            var saml2AuthnResponse = new Saml2AuthnResponse(samlConfig);

            binding.ReadSamlResponse(request.ToGenericHttpRequest(), saml2AuthnResponse);

            await sequenceLogic.ValidateSequenceAsync(binding.RelayState);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>();
            
            try
            {
                logger.ScopeTrace($"SAML Authn response '{saml2AuthnResponse.XmlDocument.OuterXml}'.");
                logger.SetScopeProperty("status", saml2AuthnResponse.Status.ToString());
                logger.ScopeTrace("Up, SAML Authn response.", triggerEvent: true);

                if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
                {
                    throw new SamlRequestException("Unsuccessful Authn response.") { RouteBinding = RouteBinding, Status = saml2AuthnResponse.Status };
                }

                binding.Unbind(request.ToGenericHttpRequest(), saml2AuthnResponse);
                logger.ScopeTrace("Up, Successful SAML Authn response.", triggerEvent: true);

                var claims = saml2AuthnResponse.ClaimsIdentity?.Claims;
                if (saml2AuthnResponse.ClaimsIdentity?.Claims?.Count() <= 0)
                {
                    throw new SamlRequestException("Empty claims collection.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }

                if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    claims = AddNameIdClaim(claims); 
                }

                //TODO validate SAML claim type and value max length

                return await AuthnResponseDownAsync(sequenceData, saml2AuthnResponse.Status, claims);
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await AuthnResponseDownAsync(sequenceData, ex.Status);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return await AuthnResponseDownAsync(sequenceData, Saml2StatusCodes.Responder);
            }
        }

        private List<Claim> AddNameIdClaim(IEnumerable<Claim> claims)
        {
            var nameIdValue = claims.FindFirstValue(c => c.Type == ClaimTypes.Upn);
            if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstValue(c => c.Type == ClaimTypes.Email);
            }
            else if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstValue(c => c.Type == ClaimTypes.Name);
            }

            var newClaims = new List<Claim>();
            if (!nameIdValue.IsNullOrEmpty())
            {
                newClaims.AddClaim(ClaimTypes.NameIdentifier, nameIdValue);
            }
            return newClaims;
        }

        private async Task<IActionResult> AuthnResponseDownAsync(SamlUpSequenceData sequenceData, Saml2StatusCodes status, IEnumerable<Claim> claims = null)
        {
            var type = sequenceData.DownPartyType.ToEnum<PartyType>();
            logger.ScopeTrace($"Response, Down type {type}.");
            switch (type)
            {
                case PartyType.OAuth2:
                    throw new NotImplementedException();
                case PartyType.Oidc:
                    if(status == Saml2StatusCodes.Success)
                    {
                        var claimsLogic = serviceProvider.GetService<ClaimsLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyId, await claimsLogic.FromSamlToJwtClaims(claims));
                    }
                    else
                    {
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyId, StatusToOAuth2OidcError(status));
                    }
                case PartyType.Saml2:
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyId, status, claims);

                default:
                    throw new NotSupportedException();
            }
        }

        private string StatusToOAuth2OidcError(Saml2StatusCodes status)
        {
            switch (status)
            {
                case Saml2StatusCodes.AuthnFailed:
                case Saml2StatusCodes.NoAuthnContext:
                case Saml2StatusCodes.NoPassive:
                    return IdentityConstants.ResponseErrors.LoginRequired;

                default:
                    return IdentityConstants.ResponseErrors.InvalidRequest;
            }
        }
    }
}
