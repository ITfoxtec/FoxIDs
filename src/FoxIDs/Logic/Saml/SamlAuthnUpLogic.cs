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
using ITfoxtec.Identity.Util;

namespace FoxIDs.Logic
{
    public class SamlAuthnUpLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly FormActionLogic formActionLogic;
        private readonly ClaimTransformationsLogic claimTransformationsLogic;
        private readonly ClaimsDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsDownLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;

        public SamlAuthnUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, FormActionLogic formActionLogic, ClaimTransformationsLogic claimTransformationsLogic, ClaimsDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsDownLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.formActionLogic = formActionLogic;
            this.claimTransformationsLogic = claimTransformationsLogic;
            this.claimsDownLogic = claimsDownLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
        }
        public async Task<IActionResult> AuthnRequestRedirectAsync(UpPartyLink partyLink, LoginRequest loginRequest)
        {
            logger.ScopeTrace("Up, SAML Authn request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await loginRequest.ValidateObjectAsync();

            await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
            {
                DownPartyId = loginRequest.DownParty.Id,
                DownPartyType = loginRequest.DownParty.Type,
                UpPartyId = partyId,
                LoginAction = loginRequest.LoginAction,
                UserId = loginRequest.UserId,
                MaxAge = loginRequest.MaxAge
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.SamlUpJumpController, Constants.Endpoints.UpJump.AuthnRequest, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> AuthnRequestAsync(string partyId)
        {
            logger.ScopeTrace("Up, SAML Authn request.");
            var samlUpSequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: false);
            if (!samlUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid up-party id.");
            }
            logger.SetScopeProperty("upPartyId", samlUpSequenceData.UpPartyId);

            var party = await tenantRepository.GetAsync<SamlUpParty>(samlUpSequenceData.UpPartyId);

            switch (party.AuthnBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await AuthnRequestAsync(party, new Saml2RedirectBinding(), samlUpSequenceData);
                case SamlBindingTypes.Post:
                    return await AuthnRequestAsync(party, new Saml2PostBinding(), samlUpSequenceData);
                default:
                    throw new NotSupportedException($"Binding '{party.AuthnBinding.RequestBinding}' not supported.");
            }
        }

        private async Task<IActionResult> AuthnRequestAsync<T>(SamlUpParty party, Saml2Binding<T> binding, SamlUpSequenceData samlUpSequenceData)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party);

            binding.RelayState = SequenceString;
            var saml2AuthnRequest = new Saml2AuthnRequest(samlConfig);

            switch (samlUpSequenceData.LoginAction)
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

            formActionLogic.AddFormActionAllowAll();

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
            if (binding.RelayState.IsNullOrEmpty()) throw new ArgumentNullException(nameof(binding.RelayState), binding.GetTypeName());

            await sequenceLogic.ValidateSequenceAsync(binding.RelayState);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>();
            
            try
            {
                logger.ScopeTrace($"SAML Authn response '{saml2AuthnResponse.XmlDocument.OuterXml}'.");
                logger.SetScopeProperty("upPartyStatus", saml2AuthnResponse.Status.ToString());
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

                var externalSessionId = claims.FindFirstValue(c => c.Type == Saml2ClaimTypes.SessionIndex);
                externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionStatedMax, nameof(externalSessionId), "Session index claim");
                claims = claims.Where(c => c.Type != Saml2ClaimTypes.SessionIndex).ToList();
                var sessionId = RandomGenerator.Generate(24);
                claims.AddClaim(Saml2ClaimTypes.SessionIndex, sessionId);

                var transformedClaims = await claimTransformationsLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                var validClaims = ValidateClaims(party, transformedClaims);

                var jwtValidClaims = await claimsDownLogic.FromSamlToJwtClaimsAsync(validClaims);
                await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, GetDownPartyLink(party, sequenceData), jwtValidClaims, sessionId, externalSessionId);

                return await AuthnResponseDownAsync(sequenceData, saml2AuthnResponse.Status, jwtValidClaims);
            }
            catch (StopSequenceException)
            {
                throw;
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

        private DownPartyLink GetDownPartyLink(UpParty upParty, SamlUpSequenceData sequenceData) => upParty.DisableSingleLogout || sequenceData.DownPartyId == null || sequenceData.DownPartyType == null ? 
            null : new DownPartyLink { Id = sequenceData.DownPartyId, Type = sequenceData.DownPartyType.Value };

        private IEnumerable<Claim> ValidateClaims(SamlUpParty party, IEnumerable<Claim> claims)
        {
            IEnumerable<string> acceptedClaims = Constants.DefaultClaims.SamlClaims.ConcatOnce(party.Claims);
            claims = claims.Where(c => acceptedClaims.Any(ic => ic == c.Type));
            foreach(var claim in claims)
            {
                if(claim.Type?.Length > Constants.Models.Claim.SamlTypeLength)
                {
                    throw new SamlRequestException($"Claim '{claim.Type.Substring(0, Constants.Models.Claim.SamlTypeLength)}' is too long, maximum length of '{Constants.Models.Claim.SamlTypeLength}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }

                if (claim.Value?.Length > Constants.Models.SamlParty.ClaimValueLength)
                {
                    throw new SamlRequestException($"Claim '{claim.Type}' value is too long, maximum length of '{Constants.Models.SamlParty.ClaimValueLength}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
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
                var claim = new Claim(ClaimTypes.NameIdentifier, $"{partyName}|{nameIdValue}");
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

        private async Task<IActionResult> AuthnResponseDownAsync(SamlUpSequenceData sequenceData, Saml2StatusCodes status, List<Claim> jwtClaims = null)
        {
            try
            {
                logger.ScopeTrace($"Response, Down type {sequenceData.DownPartyType}.");
                switch (sequenceData.DownPartyType)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        if (status == Saml2StatusCodes.Success)
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyId, jwtClaims);
                        }
                        else
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyId, StatusToOAuth2OidcError(status));
                        }
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyId, status, jwtClaims);

                    default:
                        throw new NotSupportedException();
                }
            }
            catch (Exception ex)
            {
                throw new StopSequenceException("Falling authn response down", ex);
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
