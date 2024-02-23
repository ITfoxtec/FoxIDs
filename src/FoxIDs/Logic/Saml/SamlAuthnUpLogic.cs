using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Claims;
using Saml2Http = ITfoxtec.Identity.Saml2.Http;
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
using FoxIDs.Logic.Tracks;
using FoxIDs.Infrastructure.Saml2;
using System.Xml;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Logic
{
    public class SamlAuthnUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly SamlMetadataReadUpLogic samlMetadataReadUpLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;
        private readonly PlanUsageLogic planUsageLogic;

        public SamlAuthnUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, PlanUsageLogic planUsageLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, SecurityHeaderLogic securityHeaderLogic, SamlMetadataReadUpLogic samlMetadataReadUpLogic, ClaimTransformLogic claimTransformLogic, ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.samlMetadataReadUpLogic = samlMetadataReadUpLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
            this.planUsageLogic = planUsageLogic;
        }

        public async Task<IActionResult> AuthnRequestRedirectAsync(UpPartyLink partyLink, LoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "Up, SAML Authn request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
            {
                DownPartyLink = loginRequest.DownPartyLink,
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                LoginAction = loginRequest.LoginAction,
                UserId = loginRequest.UserId,
                MaxAge = loginRequest.MaxAge,
                LoginEmailHint = loginRequest.EmailHint
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.SamlUpJumpController, Constants.Endpoints.UpJump.AuthnRequest, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult(RouteBinding.DisplayName);
        }

        public async Task<IActionResult> AuthnRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "Up, SAML Authn request.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            var samlUpSequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: false);
            if (!samlUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }

            var party = await tenantRepository.GetAsync<SamlUpParty>(samlUpSequenceData.UpPartyId);
            await samlMetadataReadUpLogic.CheckMetadataAndUpdateUpPartyAsync(party);

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

        private async Task<IActionResult> AuthnRequestAsync(SamlUpParty party, Saml2Binding binding, SamlUpSequenceData samlUpSequenceData)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSigningAndDecryptionCertificate: true);

            binding.RelayState = await sequenceLogic.CreateExternalSequenceIdAsync();
            var saml2AuthnRequest = new Saml2AuthnRequest(samlConfig);
            if (!samlUpSequenceData.LoginEmailHint.IsNullOrWhiteSpace())
            {
                saml2AuthnRequest.Subject = new Subject { NameID = new NameID { ID = samlUpSequenceData.LoginEmailHint, Format = NameIdentifierFormats.Email.OriginalString } };
            }

            switch (samlUpSequenceData.LoginAction)
            {
                case LoginAction.ReadSession:
                    saml2AuthnRequest.IsPassive = true;
                    break;
                case LoginAction.SessionUserRequireLogin:
                case LoginAction.RequireLogin:
                    saml2AuthnRequest.ForceAuthn = true;
                    break;
                default:
                    break;
            }

            if (party.AuthnContextClassReferences?.Count() > 0)
            {
                saml2AuthnRequest.RequestedAuthnContext = new RequestedAuthnContext
                {
                    Comparison = party.AuthnContextComparison.HasValue ? (AuthnContextComparisonTypes)Enum.Parse(typeof(AuthnContextComparisonTypes), party.AuthnContextComparison.Value.ToString()) : null,
                    AuthnContextClassRef = party.AuthnContextClassReferences,
                };
            }

            binding.Bind(saml2AuthnRequest);
            logger.ScopeTrace(() => $"SAML Authn request '{saml2AuthnRequest.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => $"Authn URL '{samlConfig.SingleSignOnDestination?.OriginalString}'.");
            logger.ScopeTrace(() => "Up, Sending SAML Authn request.", triggerEvent: true);

            securityHeaderLogic.AddFormActionAllowAll();

            if (binding is Saml2RedirectBinding saml2RedirectBinding)
            {
                return await saml2RedirectBinding.ToActionFormResultAsync();
            }
            else if (binding is Saml2PostBinding saml2PostBinding)
            {
                return await saml2PostBinding.ToActionFormResultAsync();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public async Task<IActionResult> AuthnResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => $"Up, SAML Authn response.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);

            var samlHttpRequest = HttpContext.Request.ToGenericHttpRequest(validate: true);
            if (samlHttpRequest.Binding is Saml2RedirectBinding || samlHttpRequest.Binding is Saml2PostBinding)
            {
                logger.ScopeTrace(() => $"Binding, configured '{party.AuthnBinding.ResponseBinding}', actual '{samlHttpRequest.Binding.GetType().Name}'");
                return await AuthnResponseAsync(party, samlHttpRequest);
            }
            else
            {
                throw new NotSupportedException($"Binding '{samlHttpRequest.Binding.GetType().Name}' not supported.");
            }
        }

        private async Task<IActionResult> AuthnResponseAsync(SamlUpParty party, Saml2Http.HttpRequest samlHttpRequest)
        {
            var request = HttpContext.Request;
            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSigningAndDecryptionCertificate: true);

            var saml2AuthnResponse = new Saml2AuthnResponse(samlConfig);
            try
            {
                samlHttpRequest.Binding.ReadSamlResponse(samlHttpRequest, saml2AuthnResponse);
            }
            catch (Exception ex)
            {
                if (samlConfig.SecondaryDecryptionCertificate != null && samlHttpRequest.Binding is Saml2PostBinding && ex.Source.Contains("cryptography", StringComparison.OrdinalIgnoreCase))
                {
                    samlConfig.DecryptionCertificates = new List<X509Certificate2> { samlConfig.SecondaryDecryptionCertificate };
                    saml2AuthnResponse = new Saml2AuthnResponse(samlConfig);
                    samlHttpRequest.Binding.ReadSamlResponse(samlHttpRequest, saml2AuthnResponse);
                    logger.ScopeTrace(() => $"SAML Authn response decrypted with secondary certificate.", traceType: TraceTypes.Message);
                }
                else
                {
                    throw;
                }
            }

            SamlUpSequenceData sequenceData = null;
            try
            {
                if (samlHttpRequest.Binding.RelayState.IsNullOrEmpty()) throw new ArgumentNullException(nameof(samlHttpRequest.Binding.RelayState), samlHttpRequest.Binding.GetTypeName());

                await sequenceLogic.ValidateExternalSequenceIdAsync(samlHttpRequest.Binding.RelayState);
                sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Invalid RelayState '{samlHttpRequest.Binding.RelayState}' returned from the IdP.");
                throw;
            }

            try
            {
                logger.ScopeTrace(() => $"SAML Authn response '{saml2AuthnResponse.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, saml2AuthnResponse.Status.ToString());
                logger.ScopeTrace(() => "Up, SAML Authn response.", triggerEvent: true);

                if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
                {
                    throw new SamlRequestException("Unsuccessful Authn response.") { RouteBinding = RouteBinding, Status = saml2AuthnResponse.Status };
                }

                try
                {
                    samlHttpRequest.Binding.Unbind(samlHttpRequest, saml2AuthnResponse);
                    logger.ScopeTrace(() => "Up, Successful SAML Authn response.", triggerEvent: true);
                }
                catch (Exception ex)
                {
                    var isex = saml2ConfigurationLogic.GetInvalidSignatureValidationCertificateException(samlConfig, ex);
                    if(isex != null) 
                    {
                        throw isex;
                    }
                    throw;
                }

                if (!(saml2AuthnResponse.ClaimsIdentity?.Claims?.Count() > 0))
                {
                    throw new SamlRequestException("Empty claims collection.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }

                var claims = new List<Claim>(saml2AuthnResponse.ClaimsIdentity.Claims.Where(c => c.Type != ClaimTypes.NameIdentifier));
                var nameIdClaim = GetNameIdClaim(party.Name, saml2AuthnResponse.ClaimsIdentity.Claims);
                if(nameIdClaim != null)
                {
                    claims.Add(nameIdClaim);
                }
                logger.ScopeTrace(() => $"Up, SAML Authn received SAML claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var externalSessionId = claims.FindFirstOrDefaultValue(c => c.Type == Saml2ClaimTypes.SessionIndex);
                externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionIdMax, nameof(externalSessionId), "Session index claim");
                claims = claims.Where(c => c.Type != Saml2ClaimTypes.SessionIndex && c.Type != Constants.SamlClaimTypes.UpParty && c.Type != Constants.SamlClaimTypes.UpPartyType).ToList();
                claims.AddClaim(Constants.SamlClaimTypes.UpParty, party.Name);
                claims.AddClaim(Constants.SamlClaimTypes.UpPartyType, party.Type.ToString().ToLower());

                var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                var validClaims = ValidateClaims(party, transformedClaims);
                logger.ScopeTrace(() => $"Up, SAML Authn output SAML claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var jwtValidClaims = await claimsOAuthDownLogic.FromSamlToJwtClaimsAsync(validClaims);
                var sessionId = await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, party.DisableSingleLogout ? null : sequenceData.DownPartyLink, jwtValidClaims, externalSessionId);
                if (!sessionId.IsNullOrEmpty())
                {
                    jwtValidClaims.AddClaim(JwtClaimTypes.SessionId, sessionId);
                }

                if (!sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
                {
                    await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), PartyTypes.Saml2);
                }

                logger.ScopeTrace(() => $"Up, SAML Authn output JWT claims '{jwtValidClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                return await AuthnResponseDownAsync(sequenceData, saml2AuthnResponse.Status, jwtValidClaims);
            }
            catch (StopSequenceException)
            {
                throw;
            }
            catch (SamlRequestException ex)
            {
                if (sequenceData == null)
                {
                    throw new StopSequenceException("SequenceData is null. Probably caused by invalid RelayState returned from the IdP.", ex);
                }
                logger.Error(ex);
                return await AuthnResponseDownAsync(sequenceData, ex.Status);
            }
            catch (Exception ex)
            {
                if (sequenceData == null)
                {
                    throw new StopSequenceException("SequenceData is null. Probably caused by invalid RelayState returned from the IdP.", ex);
                }
                logger.Error(ex);
                return await AuthnResponseDownAsync(sequenceData, Saml2StatusCodes.Responder);
            }
        }

        private IEnumerable<Claim> ValidateClaims(SamlUpParty party, IEnumerable<Claim> claims)
        {
            var acceptAllClaims = party.Claims?.Where(c => c == "*")?.Count() > 0;
            if (!acceptAllClaims)
            {
                var acceptedClaims = Constants.DefaultClaims.SamlClaims.ConcatOnce(party.Claims);
                claims = claims.Where(c => acceptedClaims.Any(ic => ic == c.Type));
            }
            var totalValueLenght = 0;
            foreach (var claim in claims)
            {
                if(claim.Type?.Length > Constants.Models.Claim.SamlTypeLength)
                {
                    throw new SamlRequestException($"Claim '{claim.Type.Substring(0, Constants.Models.Claim.SamlTypeLength)}' is too long, maximum length of '{Constants.Models.Claim.SamlTypeLength}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }

                if (claim.Value?.Length > Constants.Models.Claim.ProcessValueLength)
                {
                    throw new SamlRequestException($"Claim '{claim.Type}' value is too long, maximum length of '{Constants.Models.Claim.ProcessValueLength}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }
                if (claim.Value?.Length > 0)
                {
                    totalValueLenght += claim.Value.Length;
                }
            }
            if (totalValueLenght > Constants.Models.Claim.ProcessValueLength)
            {
                throw new SamlRequestException($"The total length of all claim values combined is too long, maximum length of '{Constants.Models.Claim.ProcessValueLength}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
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
                nameIdValue = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.Upn);
            }
            
            if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.Email);
            }
            
            if (nameIdValue.IsNullOrEmpty())
            {
                nameIdValue = claims.FindFirstOrDefaultValue(c => c.Type == ClaimTypes.Name);
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
                logger.ScopeTrace(() => $"Response, Down type {sequenceData.DownPartyLink.Type}.");

                if (status == Saml2StatusCodes.Success)
                {
                    planUsageLogic.LogLoginEvent(PartyTypes.Saml2);
                }

                switch (sequenceData.DownPartyLink.Type)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        if (status == Saml2StatusCodes.Success)
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(sequenceData.DownPartyLink.Id, jwtClaims);
                        }
                        else
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.DownPartyLink.Id, StatusToOAuth2OidcError(status));
                        }
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.DownPartyLink.Id, status, jwtClaims);
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(sequenceData.DownPartyLink.Id, jwtClaims, error: status == Saml2StatusCodes.Success ? null : StatusToOAuth2OidcError(status));
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

        public static (string issuer, IEnumerable<string> audiences) ReadTokenExchangeSubjectTokenIssuerAndAudiencesAsync(string subjectToken)
        {
            var binding = new FoxIdsSaml2TokenExchangeBinding();
            var saml2TokenExchangeRequest = new FoxIdsSaml2TokenExchangeRequest(new Saml2Configuration { AudienceRestricted = false });
            binding.ReadSamlRequest(GetHttpRequest(subjectToken), saml2TokenExchangeRequest);

            return (saml2TokenExchangeRequest.Issuer, ReadTokenExchangeSubjectTokenAudiences(subjectToken));
        }

        private static IEnumerable<string> ReadTokenExchangeSubjectTokenAudiences(string subjectToken)
        {
            var xmlDocument = subjectToken.ToXmlDocument();
            var audienceElements = xmlDocument.DocumentElement.SelectNodes($"//*[local-name()='Audience']");
            if (!(audienceElements?.Count > 0))
            {
                throw new Saml2RequestException("There is not at leas one audience element.");
            }

            foreach (XmlNode audienceElement in audienceElements)
            {
                yield return audienceElement.InnerText;
            }
        }

        public async Task<List<Claim>> ValidateTokenExchangeSubjectTokenAsync(UpPartyLink partyLink, string subjectToken)
        {
            logger.ScopeTrace(() => "Up, SAML validate token exchange subject token.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);

            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSignatureValidationCertificates: true);

            var binding = new FoxIdsSaml2TokenExchangeBinding();
            var saml2TokenExchangeRequest = new FoxIdsSaml2TokenExchangeRequest(samlConfig);
            binding.Unbind(GetHttpRequest(subjectToken), saml2TokenExchangeRequest);
            logger.ScopeTrace(() => "Up, SAML validate token exchange request accepted.", triggerEvent: true);

            var principal = new ClaimsPrincipal(saml2TokenExchangeRequest.ClaimsIdentity);

            if (principal.Identity == null || !principal.Identity.IsAuthenticated)
            {
                throw new InvalidOperationException("No Claims Identity created from SAML2 Response.");
            }

            var receivedClaims = principal.Identities.First().Claims;
            logger.ScopeTrace(() => "Up, SAML token exchange subject token valid.", triggerEvent: true);
            logger.ScopeTrace(() => $"Up, SAML received JWT claims '{receivedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var claims = receivedClaims.Where(c => c.Type != Constants.SamlClaimTypes.UpParty && c.Type != Constants.SamlClaimTypes.UpPartyType).ToList();
            claims.AddClaim(Constants.SamlClaimTypes.UpParty, party.Name);
            claims.AddClaim(Constants.SamlClaimTypes.UpPartyType, party.Type.ToString().ToLower());

            var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            var validClaims = ValidateClaims(party, transformedClaims);
            logger.ScopeTrace(() => $"Up, SAML output SAML claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var jwtValidClaims = await claimsOAuthDownLogic.FromSamlToJwtClaimsAsync(validClaims);

            logger.ScopeTrace(() => $"Up, SAML output JWT claims '{jwtValidClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return jwtValidClaims;
        }

        private static Saml2Http.HttpRequest GetHttpRequest(string subjectToken)
        {
            return new Saml2Http.HttpRequest { Method = "DIRECT", Body = subjectToken };
        }
    }
}
