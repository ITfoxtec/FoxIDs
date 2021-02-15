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
using ITfoxtec.Identity.Saml2.Claims;

namespace FoxIDs.Logic
{
    public class SamlAuthnUpLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly FormActionLogic formActionLogic;
        private readonly ClaimTransformationsLogic claimTransformationsLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;

        public SamlAuthnUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, FormActionLogic formActionLogic, ClaimTransformationsLogic claimTransformationsLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.formActionLogic = formActionLogic;
            this.claimTransformationsLogic = claimTransformationsLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
        }

        public async Task<IActionResult> AuthnRequestAsync(UpPartyLink partyLink, LoginRequest loginRequest)
        {
            logger.ScopeTrace("Up, SAML Authn request.");
            var partyId = await UpParty.IdFormat(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await loginRequest.ValidateObjectAsync();

            await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
            {
                DownPartyId = loginRequest.DownParty.Id,
                DownPartyType = loginRequest.DownParty.Type,
            });

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);

            switch (party.AuthnBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await AuthnRequestAsync(party, new Saml2RedirectBinding(), loginRequest);
                case SamlBindingTypes.Post:
                    return await AuthnRequestAsync(party, new Saml2PostBinding(), loginRequest);
                default:
                    throw new NotSupportedException($"Binding '{party.AuthnBinding.RequestBinding}' not supported.");
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
                case LoginAction.RequireLogin:
                    saml2AuthnRequest.ForceAuthn = true;
                    break;
                default:
                    break;
            }

            binding.Bind(saml2AuthnRequest);
            logger.ScopeTrace($"SAML Authn request '{saml2AuthnRequest.XmlDocument.OuterXml}'.");
            logger.ScopeTrace($"Authn URL '{samlConfig.SingleSignOnDestination?.OriginalString}'.");
            logger.ScopeTrace("Up, Sending SAML Authn request.", triggerEvent: true);

            if (binding is Saml2Binding<Saml2RedirectBinding>)
            {
                return await Task.FromResult((binding as Saml2RedirectBinding).ToActionResult());
            }
            if (binding is Saml2Binding<Saml2PostBinding>)
            {
                await formActionLogic.AddFormActionByUrlAsync(samlConfig.SingleSignOnDestination.OriginalString);
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

            logger.ScopeTrace($"Binding '{party.AuthnBinding.ResponseBinding}'");
            switch (party.AuthnBinding.ResponseBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await AuthnResponseAsync(party, new Saml2RedirectBinding());
                case SamlBindingTypes.Post:
                    return await AuthnResponseAsync(party, new Saml2PostBinding());
                default:
                    throw new NotSupportedException($"SAML binding '{party.AuthnBinding.ResponseBinding}' not supported.");
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

                if (saml2AuthnResponse.ClaimsIdentity?.Claims?.Count() <= 0)
                {
                    throw new SamlRequestException("Empty claims collection.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }

                var claims = new List<Claim>(saml2AuthnResponse.ClaimsIdentity.Claims.Where(c => c.Type != ClaimTypes.NameIdentifier));
                var nameIdClaim = GetNameIdClaim(party.Name, saml2AuthnResponse.ClaimsIdentity.Claims);
                if(nameIdClaim != null)
                {
                    claims.Add(nameIdClaim);
                }

                var transformedClaims = await claimTransformationsLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                var validClaims = ValidateClaims(party, transformedClaims);

                return await AuthnResponseDownAsync(sequenceData, saml2AuthnResponse.Status, validClaims);
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

        private IEnumerable<Claim> ValidateClaims(SamlUpParty party, IEnumerable<Claim> claims)
        {
            IEnumerable<string> acceptedClaims = Constants.DefaultClaims.SamlClaims.ConcatOnce(party.Claims);
            claims = claims.Where(c => acceptedClaims.Any(ic => ic == c.Type));
            foreach(var claim in claims)
            {
                if(claim.Type?.Count() > Constants.Models.Claim.SamlTypeLength)
                {
                    throw new SamlRequestException($"Claim '{claim.Type.Substring(0, Constants.Models.Claim.SamlTypeLength)}' is too long.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }
                if (claim.Value?.Count() > Constants.Models.SamlParty.ClaimValueLength)
                {
                    throw new SamlRequestException($"Claim value '{claim.Value.Substring(0, Constants.Models.SamlParty.ClaimValueLength)}' is too long.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }
            }
            return claims;
        }

        private Claim GetNameIdClaim(string partyName, IEnumerable<Claim> claims)
        {
            var nameIdValue = string.Empty;
            var nameIdFormat = string.Empty;

            var nameIdClaim = claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault();
            if(nameIdClaim != null)
            {
                nameIdValue = nameIdClaim.Value;
                nameIdFormat = nameIdClaim.Properties.Where(p => p.Key == Saml2ClaimTypes.NameIdFormat).Select(p => p.Value).FirstOrDefault();
            }
            
            if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstValue(c => c.Type == ClaimTypes.Upn);
            }
            
            if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstValue(c => c.Type == ClaimTypes.Email);
            }
            
            if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstValue(c => c.Type == ClaimTypes.Name);
            }

            if (!nameIdValue.IsNullOrEmpty())
            {
                var partyNameIdValue = $"{partyName}|{nameIdValue}";
                var claim = new Claim(ClaimTypes.NameIdentifier, partyNameIdValue);
                if (!nameIdFormat.IsNullOrEmpty())
                {
                    claim.Properties.Add(Saml2ClaimTypes.NameIdFormat, nameIdFormat);
                }
                return claim;
            }
            else
            {
                return null;
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
            logger.ScopeTrace($"Response, Down type {sequenceData.DownPartyType}.");
            switch (sequenceData.DownPartyType)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    if(status == Saml2StatusCodes.Success)
                    {
                        var claimsLogic = serviceProvider.GetService<ClaimsLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyId, await claimsLogic.FromSamlToJwtClaimsAsync(claims));
                    }
                    else
                    {
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyId, StatusToOAuth2OidcError(status));
                    }
                case PartyTypes.Saml2:
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
