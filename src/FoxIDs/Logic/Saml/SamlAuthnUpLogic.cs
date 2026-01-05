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
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Security.Claims;
using System.Threading.Tasks;
using FoxIDs.Models.Sequences;
using FoxIDs.Logic.Tracks;
using FoxIDs.Infrastructure.Saml2;
using ITfoxtec.Identity.Util;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace FoxIDs.Logic
{
    public class SamlAuthnUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly SamlMetadataReadUpLogic samlMetadataReadUpLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ExtendedUiLogic extendedUiLogic;
        private readonly ExternalUserLogic externalUserLogic;
        private readonly ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly AuditLogic auditLogic;

        public SamlAuthnUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, PlanUsageLogic planUsageLogic, AuditLogic auditLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, SecurityHeaderLogic securityHeaderLogic, SamlMetadataReadUpLogic samlMetadataReadUpLogic, ClaimTransformLogic claimTransformLogic, ExtendedUiLogic extendedUiLogic, ExternalUserLogic externalUserLogic, ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.samlMetadataReadUpLogic = samlMetadataReadUpLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.extendedUiLogic = extendedUiLogic;
            this.externalUserLogic = externalUserLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
            this.planUsageLogic = planUsageLogic;
            this.auditLogic = auditLogic;
        }

        public async Task<IActionResult> AuthnRequestRedirectAsync(UpPartyLink partyLink, ILoginRequest loginRequest, string hrdLoginUpPartyName = null)
        {
            logger.ScopeTrace(() => "AuthMethod, SAML Authn request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            planUsageLogic.LogLoginEvent(PartyTypes.Saml2);

            await loginRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData(loginRequest)
            {
                HrdLoginUpPartyName = hrdLoginUpPartyName,
                UpPartyId = partyId,
                UpPartyProfileName = partyLink.ProfileName,
            }, partyName: party.Name);

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.SamlUpJumpController, Constants.Endpoints.UpJump.AuthnRequest, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult();
        }

        public async Task<IActionResult> AuthnRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, SAML Authn request.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            var samlUpSequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(partyName: partyId.PartyIdToName(), remove: false);
            if (!samlUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(samlUpSequenceData.UpPartyId);
            if (party.EnableIdPInitiated)
            {
                var sessionId = await sessionUpPartyLogic.GetOrCreateSessionIdAsync(party);
                if (!sessionId.IsNullOrEmpty())
                {
                    var idPInitiatedTtlGrant = await serviceProvider.GetService<SamlAuthnUpIdPInitiatedGrantLogic>().GetGrantAsync(party, sessionId);

                    if (idPInitiatedTtlGrant != null && idPInitiatedTtlGrant.DownPartyId == samlUpSequenceData.DownPartyLink?.Id && idPInitiatedTtlGrant.DownPartyType == samlUpSequenceData.DownPartyLink?.Type)
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<SamlUpSequenceData>();
                        var claims = idPInitiatedTtlGrant.Claims.ToClaimList();
                        logger.ScopeTrace(() => $"AuthMethod, SAML Authn output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                        return await AuthnResponseDownAsync(samlUpSequenceData, Saml2StatusCodes.Success, claims);
                    }
                }
            }
            party = await samlMetadataReadUpLogic.CheckMetadataAndUpdateUpPartyAsync(party);

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
            if (!party.DisableLoginHint && !samlUpSequenceData.LoginHint.IsNullOrWhiteSpace())
            {
                saml2AuthnRequest.Subject = new Subject { NameID = new NameID { ID = samlUpSequenceData.LoginHint, Format = new EmailAddressAttribute().IsValid(samlUpSequenceData.LoginHint) ? NameIdentifierFormats.Email.OriginalString : NameIdentifierFormats.Persistent.OriginalString } };
            }

            saml2AuthnRequest.AssertionConsumerServiceUrl = new Uri(UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlAcs));

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

            var profile = GetProfile(party, samlUpSequenceData);

            if (party.AuthnContextClassReferences?.Count() > 0)
            {
                saml2AuthnRequest.RequestedAuthnContext = new RequestedAuthnContext
                {
                    Comparison = party.AuthnContextComparison.HasValue ? (AuthnContextComparisonTypes)Enum.Parse(typeof(AuthnContextComparisonTypes), party.AuthnContextComparison.Value.ToString()) : null,
                    AuthnContextClassRef = party.AuthnContextClassReferences,
                };
            }
            if(profile != null && (profile.AuthnContextComparison.HasValue && party.AuthnContextClassReferences?.Count() > 0 || profile.AuthnContextClassReferences?.Count() > 0))
            {
                if(saml2AuthnRequest.RequestedAuthnContext == null)
                {
                    saml2AuthnRequest.RequestedAuthnContext = new RequestedAuthnContext();
                }

                if(profile.AuthnContextComparison.HasValue)
                {
                    saml2AuthnRequest.RequestedAuthnContext.Comparison = (AuthnContextComparisonTypes)Enum.Parse(typeof(AuthnContextComparisonTypes), profile.AuthnContextComparison.Value.ToString());
                }
                if(profile.AuthnContextClassReferences?.Count() > 0)
                {
                    saml2AuthnRequest.RequestedAuthnContext.AuthnContextClassRef = profile.AuthnContextClassReferences;
                }
            }

            if (!party.AuthnRequestExtensionsXml.IsNullOrWhiteSpace())
            {
                try
                {
                    saml2AuthnRequest.Extensions = new Extensions();
                    foreach (var element in ParseAuthnRequestExtensionsXml(party.AuthnRequestExtensionsXml))
                    {
                        saml2AuthnRequest.Extensions.Element.Add(element);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Unable to parse and add extensions XML. A valid XML string is required.");
                }            
            }
            if (profile != null && !profile.AuthnRequestExtensionsXml.IsNullOrWhiteSpace())
            {
                try
                {
                    saml2AuthnRequest.Extensions = new Extensions();
                    foreach (var element in ParseAuthnRequestExtensionsXml(profile.AuthnRequestExtensionsXml))
                    {
                        saml2AuthnRequest.Extensions.Element.Add(element);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Unable to parse and add profile extensions XML. A valid XML string is required.");
                }
            }

            binding.Bind(saml2AuthnRequest);
            logger.ScopeTrace(() => $"SAML Authn request '{saml2AuthnRequest.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => $"Authn URL '{samlConfig.SingleSignOnDestination?.OriginalString}'.");
            logger.ScopeTrace(() => "AuthMethod, Sending SAML Authn request.", triggerEvent: true);

            securityHeaderLogic.AddFormActionAllowAll();

            return binding.ToSamlActionResult();
        }

        private static IReadOnlyCollection<System.Xml.Linq.XElement> ParseAuthnRequestExtensionsXml(string authnRequestExtensionsXml)
        {
            var wrapper = System.Xml.Linq.XElement.Parse($"<root>{authnRequestExtensionsXml}</root>");
            return wrapper.Elements().ToList();
        }

        private SamlUpPartyProfile GetProfile(SamlUpParty party, SamlUpSequenceData samlUpSequenceData)
        {
            if (!samlUpSequenceData.UpPartyProfileName.IsNullOrEmpty() && party.Profiles != null)
            {
                return party.Profiles.Where(p => p.Name == samlUpSequenceData.UpPartyProfileName).FirstOrDefault();
            }
            return null;
        }

        public async Task<IActionResult> AuthnResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => $"AuthMethod, SAML Authn response.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(partyId);

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
                if (samlConfig.SecondaryDecryptionCertificate != null && samlHttpRequest.Binding is Saml2PostBinding && ex.Message.Contains("decrypt", StringComparison.OrdinalIgnoreCase))
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

            logger.ScopeTrace(() => $"SAML Authn response '{saml2AuthnResponse.XmlDocument?.OuterXml}'.", traceType: TraceTypes.Message);
            logger.SetScopeProperty(Constants.Logs.UpPartyStatus, saml2AuthnResponse.Status.ToString());
            logger.ScopeTrace(() => "AuthMethod, SAML Authn response.", triggerEvent: true);

            (var sequenceData, var idPInitiatedLink) = await GetSequenceOrIdPInitiatedLink(party, samlHttpRequest, saml2AuthnResponse);

            try
            {
                if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
                {
                    throw new SamlRequestException("Unsuccessful Authn response.") { RouteBinding = RouteBinding, Status = saml2AuthnResponse.Status };
                }

                try
                {
                    samlHttpRequest.Binding.Unbind(samlHttpRequest, saml2AuthnResponse);
                    logger.ScopeTrace(() => "AuthMethod, Successful SAML Authn response.", triggerEvent: true);
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

                if (!(saml2AuthnResponse.ClaimsIdentity?.Claims?.Count() > 0))
                {
                    throw new SamlRequestException("Empty claims collection.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
                }

                var claims = new List<Claim>(saml2AuthnResponse.ClaimsIdentity.Claims.Where(c => c.Type != ClaimTypes.NameIdentifier));
                var nameIdClaim = GetNameIdClaim(party.Name, saml2AuthnResponse.ClaimsIdentity.Claims);
                if (nameIdClaim != null)
                {
                    claims.Add(nameIdClaim);
                }
                logger.ScopeTrace(() => $"AuthMethod, SAML Authn received SAML claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var externalSessionId = claims.FindFirstOrDefaultValue(c => c.Type == Saml2ClaimTypes.SessionIndex);
                externalSessionId.ValidateMaxLength(IdentityConstants.MessageLength.SessionIdMax, nameof(externalSessionId), "Session index claim");
                claims = claims.Where(c => c.Type != Saml2ClaimTypes.SessionIndex && c.Type != Saml2ClaimTypes.NameId &&
                    c.Type != Constants.SamlClaimTypes.AuthMethod && c.Type != Constants.SamlClaimTypes.AuthProfileMethod && c.Type != Constants.SamlClaimTypes.AuthMethodType &&
                    c.Type != Constants.SamlClaimTypes.UpParty && c.Type != Constants.SamlClaimTypes.UpPartyType).ToList();
                claims.AddClaim(Constants.SamlClaimTypes.AuthMethod, party.Name);
                if (!string.IsNullOrEmpty(sequenceData?.UpPartyProfileName))
                {
                    claims.AddClaim(Constants.SamlClaimTypes.AuthProfileMethod, sequenceData.UpPartyProfileName);
                }
                claims.AddClaim(Constants.SamlClaimTypes.AuthMethodType, party.Type.GetPartyTypeValue());
                claims.AddClaim(Constants.SamlClaimTypes.UpParty, party.Name);
                claims.AddClaim(Constants.SamlClaimTypes.UpPartyType, party.Type.GetPartyTypeValue());

                if (sequenceData != null)
                {
                    await sessionUpPartyLogic.CreateOrUpdateMarkerSessionAsync(party, sequenceData.DownPartyLink, externalSessionId, samlClaims: claims);
                }

                (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
                if (actionResult != null)
                {
                    if (sequenceData != null)
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<SamlUpSequenceData>(partyName: party.Name);
                    }
                    return actionResult;
                }
                var validClaims = ValidateClaims(party, transformedClaims);
                logger.ScopeTrace(() => $"AuthMethod, SAML Authn transformed SAML claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                var jwtValidClaims = await claimsOAuthDownLogic.FromSamlToJwtClaimsAsync(validClaims);

                var extendedUiActionResult = await HandleExtendedUiAsync(party, sequenceData, jwtValidClaims, externalSessionId);
                if (extendedUiActionResult != null)
                {
                    return extendedUiActionResult;
                }

                (var externalUserClaims, var externalUserActionResult) = await HandleExternalUserAsync(party, sequenceData, jwtValidClaims, externalSessionId);
                if (externalUserActionResult != null)
                {
                    return externalUserActionResult;
                }

                if (sequenceData != null)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<SamlUpSequenceData>(partyName: party.Name);
                }
                return await AuthnResponsePostAsync(party, sequenceData, jwtValidClaims, externalUserClaims, externalSessionId, idPInitiatedLink: idPInitiatedLink);
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
                return await AuthnResponseDownAsync(sequenceData, ex.Status, idPInitiatedLink: idPInitiatedLink);
            }
            catch (Exception ex)
            {
                if (sequenceData == null)
                {
                    throw new StopSequenceException("SequenceData is null. Probably caused by invalid RelayState returned from the IdP.", ex);
                }
                logger.Error(ex);
                return await AuthnResponseDownAsync(sequenceData, Saml2StatusCodes.Responder, idPInitiatedLink: idPInitiatedLink);
            }
        }

        private async Task<IActionResult> HandleExtendedUiAsync(SamlUpParty party, SamlUpSequenceData sequenceData, IEnumerable<Claim> jwtValidClaims, string externalSessionId)
        {
            var extendedUiActionResult = await extendedUiLogic.HandleUiAsync(party, sequenceData, jwtValidClaims,
                (extendedUiUpSequenceData) =>
                {
                    extendedUiUpSequenceData.ExternalSessionId = externalSessionId;
                });

            return extendedUiActionResult;
        }

        private async Task<(IEnumerable<Claim>, IActionResult)> HandleExternalUserAsync(SamlUpParty party, SamlUpSequenceData sequenceData, IEnumerable<Claim> jwtValidClaims, string externalSessionId)
        {
            (var externalUserClaims, var externalUserActionResult, var deleteSequenceData) = await externalUserLogic.HandleUserAsync(party, sequenceData, jwtValidClaims,
                (externalUserUpSequenceData) =>
                {
                    externalUserUpSequenceData.ExternalSessionId = externalSessionId;
                },
                (errorMessage) => throw new SamlRequestException(errorMessage) { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder });

            if (externalUserActionResult != null)
            {
                if (deleteSequenceData && sequenceData != null)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<SamlUpSequenceData>(partyName: party.Name);
                }
            }

            return (externalUserClaims, externalUserActionResult);
        }

        private async Task<(SamlUpSequenceData sequenceData, IdPInitiatedDownPartyLink idPInitiatedLink)> GetSequenceOrIdPInitiatedLink(SamlUpParty party, Saml2Http.HttpRequest samlHttpRequest, Saml2AuthnResponse saml2AuthnResponse)
        {
            try
            {
                if (samlHttpRequest.Binding.RelayState.IsNullOrEmpty())
                {
                    if (party.EnableIdPInitiated)
                    {
                        throw new ArgumentNullException(nameof(samlHttpRequest.Binding.RelayState), $"The {nameof(samlHttpRequest.Binding.RelayState)} contains the requested application and it is required for IdP-Initiated login. Binding: {samlHttpRequest.Binding.GetTypeName()}.");
                    }
                    else
                    {
                        throw new ArgumentNullException(nameof(samlHttpRequest.Binding.RelayState), $"The {nameof(samlHttpRequest.Binding.RelayState)} contains the sequence ID and it is required. Binding: {samlHttpRequest.Binding.GetTypeName()}.{(saml2AuthnResponse.Status == Saml2StatusCodes.Success ? " IdP-Initiated login is not enabled." : string.Empty)}");
                    }
                }

                if (party.EnableIdPInitiated && samlHttpRequest.Binding.RelayState?.StartsWith("app_name=") == true)
                {
                    var idPInitiatedLink = new IdPInitiatedDownPartyLink
                    {
                        UpPartyName = party.Name,
                        UpPartyType = party.Type
                    };

                    var rsSplit = samlHttpRequest.Binding.RelayState.Split('&');
                    if (!(rsSplit.Count() >= 2))
                    {
                        throw new Exception($"Invalid IdP-Initiated login relay state '{samlHttpRequest.Binding.RelayState}', should contain two or three elements.");
                    }

                    idPInitiatedLink.DownPartyId = await DownParty.IdFormatAsync(RouteBinding, rsSplit[0].Substring("app_name=".Count()));

                    if (rsSplit[1].Equals("app_type=saml2"))
                    {
                        idPInitiatedLink.DownPartyType = PartyTypes.Saml2;
                    }
                    else if (rsSplit[1].Equals("app_type=oidc"))
                    {
                        idPInitiatedLink.DownPartyType = PartyTypes.Oidc;
                        if (!(rsSplit.Count() >= 3))
                        {
                            throw new Exception($"Invalid IdP-Initiated login relay state '{samlHttpRequest.Binding.RelayState}', should contain three elements for OpenID Connect.");
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid 'app_type' in IdP-Initiated relay state '{samlHttpRequest.Binding.RelayState}'.");
                    }

                    if (rsSplit.Count() >= 3)
                    {
                        if (!rsSplit[2].StartsWith("app_redirect="))
                        {
                            throw new Exception($"Invalid IdP-Initiated login relay state '{samlHttpRequest.Binding.RelayState}', the third elements should be 'app_redirect'.");
                        }
                        idPInitiatedLink.DownPartyRedirectUrl = HttpUtility.UrlDecode(rsSplit[2].Substring("app_redirect=".Count()));
                    }
                    return (null,  idPInitiatedLink);
                }
                else
                {
                    if (samlHttpRequest.Binding.RelayState?.StartsWith("app_name=") == true)
                    {
                        throw new Exception("IdP-Initiated login is not enabled.");
                    }

                    await sequenceLogic.ValidateExternalSequenceIdAsync(samlHttpRequest.Binding.RelayState);
                    return (await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(partyName: party.Name, remove: false), null);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid RelayState '{samlHttpRequest.Binding.RelayState}' from the external IdP.", ex);
            }
        }

        public async Task<IActionResult> AuthnResponsePostExtendedUiAsync(ExtendedUiUpSequenceData extendedUiSequenceData, IEnumerable<Claim> jwtValidClaims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(partyName: extendedUiSequenceData.UpPartyId.PartyIdToName(), remove: false);
            var party = await tenantDataRepository.GetAsync<SamlUpParty>(extendedUiSequenceData.UpPartyId);

            try
            {
                (var externalUserClaims, var externalUserActionResult) = await HandleExternalUserAsync(party, sequenceData, jwtValidClaims, extendedUiSequenceData.ExternalSessionId);
                if (externalUserActionResult != null)
                {
                    return externalUserActionResult;
                }

                await sequenceLogic.RemoveSequenceDataAsync<SamlUpSequenceData>(partyName: party.Name);
                return await AuthnResponsePostAsync(party, sequenceData, jwtValidClaims, externalUserClaims, extendedUiSequenceData.ExternalSessionId);
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

        public async Task<IActionResult> AuthnResponsePostExternalUserAsync(ExternalUserUpSequenceData externalUserSequenceData, IEnumerable<Claim> externalUserClaims)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(partyName: externalUserSequenceData.UpPartyId.PartyIdToName(), remove: true);
            var party = await tenantDataRepository.GetAsync<SamlUpParty>(externalUserSequenceData.UpPartyId);

            try
            {
                return await AuthnResponsePostAsync(party, sequenceData, externalUserSequenceData.Claims?.ToClaimList(), externalUserClaims, externalUserSequenceData.ExternalSessionId);
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

        private async Task<IActionResult> AuthnResponsePostAsync(SamlUpParty party, SamlUpSequenceData sequenceData, IEnumerable<Claim> jwtValidClaims, IEnumerable<Claim> externalUserClaims, string externalSessionId, IdPInitiatedDownPartyLink idPInitiatedLink = null)
        {
            jwtValidClaims = externalUserLogic.AddExternalUserClaims(party, jwtValidClaims, externalUserClaims);

            (var transformedJwtClaims, var actionResult) = await claimTransformLogic.TransformAsync(party.ExitClaimTransforms?.ConvertAll(t => (ClaimTransform)t), jwtValidClaims, sequenceData);
            if (actionResult != null)
            {
                return actionResult;
            }

            var sessionId = await sessionUpPartyLogic.CreateOrUpdateSessionAsync(party, transformedJwtClaims, externalSessionId);
            if (!sessionId.IsNullOrEmpty())
            {
                transformedJwtClaims.AddOrReplaceClaim(JwtClaimTypes.SessionId, sessionId);
                if (idPInitiatedLink != null && idPInitiatedLink.DownPartyType == PartyTypes.Oidc && party.IdPInitiatedGrantLifetime > 0)
                {
                    await serviceProvider.GetService<SamlAuthnUpIdPInitiatedGrantLogic>().CreateGrantAsync(party, sessionId, transformedJwtClaims, idPInitiatedLink);
                }
            }

            if (sequenceData != null)
            {
                await hrdLogic.SaveHrdSelectionAsync(sequenceData.HrdLoginUpPartyName, sequenceData.UpPartyId.PartyIdToName(), sequenceData.UpPartyProfileName, PartyTypes.Saml2);
            }

            logger.ScopeTrace(() => $"AuthMethod, SAML Authn output JWT claims '{transformedJwtClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return await AuthnResponseDownAsync(sequenceData, jwtClaims: transformedJwtClaims, idPInitiatedLink: idPInitiatedLink);
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

        private async Task<IActionResult> AuthnResponseDownAsync(SamlUpSequenceData sequenceData, Saml2StatusCodes status = Saml2StatusCodes.Success, List<Claim> jwtClaims = null, IdPInitiatedDownPartyLink idPInitiatedLink = null)
        {
            try
            {
                var downPartyId = sequenceData != null ? sequenceData.DownPartyLink.Id : idPInitiatedLink.DownPartyId;
                var downPartyType = sequenceData != null ? sequenceData.DownPartyLink.Type : idPInitiatedLink.DownPartyType;

                logger.ScopeTrace(() => $"Response, Application type {downPartyType}.");

                if (status == Saml2StatusCodes.Success  && jwtClaims != null)
                {
                    auditLogic.LogLoginEvent(PartyTypes.Saml2, sequenceData.UpPartyId, jwtClaims);
                }

                switch (downPartyType)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        if (status == Saml2StatusCodes.Success)
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseAsync(downPartyId, jwtClaims, idPInitiatedLink: idPInitiatedLink);
                        }
                        else
                        {
                            return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(downPartyId, StatusToOAuth2OidcError(status));
                        }
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(downPartyId, status, jwtClaims, idPInitiatedLink: idPInitiatedLink);
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(downPartyId, jwtClaims, error: status == Saml2StatusCodes.Success ? null : StatusToOAuth2OidcError(status));
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

        public async Task<IEnumerable<Claim>> ValidateTokenExchangeSubjectTokenAsync(UpPartyLink partyLink, string subjectToken)
        {
            logger.ScopeTrace(() => "AuthMethod, SAML validate token exchange subject token.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(partyId);

            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSignatureValidationCertificates: true);

            var binding = new FoxIdsSaml2TokenExchangeBinding();
            var saml2TokenExchangeRequest = new FoxIdsSaml2TokenExchangeRequest(samlConfig);
            binding.Unbind(GetHttpRequest(subjectToken), saml2TokenExchangeRequest);
            logger.ScopeTrace(() => "AuthMethod, SAML validate token exchange request accepted.", triggerEvent: true);

            var principal = new ClaimsPrincipal(saml2TokenExchangeRequest.ClaimsIdentity);

            if (principal.Identity == null || !principal.Identity.IsAuthenticated)
            {
                throw new InvalidOperationException("No Claims Identity created from SAML2 Response.");
            }

            var receivedClaims = principal.Identities.First().Claims;
            logger.ScopeTrace(() => "AuthMethod, SAML token exchange subject token valid.", triggerEvent: true);
            logger.ScopeTrace(() => $"AuthMethod, SAML received JWT claims '{receivedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var claims = receivedClaims.Where(c => c.Type != Constants.SamlClaimTypes.AuthMethod && c.Type != Constants.SamlClaimTypes.AuthProfileMethod && c.Type != Constants.SamlClaimTypes.AuthMethodType && c.Type != Constants.SamlClaimTypes.UpParty && c.Type != Constants.SamlClaimTypes.UpPartyType).ToList();
            claims.AddClaim(Constants.SamlClaimTypes.AuthMethod, party.Name);
            if (!partyLink.ProfileName.IsNullOrEmpty())
            {
                claims.AddClaim(Constants.SamlClaimTypes.AuthProfileMethod, partyLink.ProfileName);
            }
            claims.AddClaim(Constants.SamlClaimTypes.AuthMethodType, party.Type.GetPartyTypeValue());
            claims.AddClaim(Constants.SamlClaimTypes.UpParty, party.Name);
            claims.AddClaim(Constants.SamlClaimTypes.UpPartyType, party.Type.GetPartyTypeValue());

            var transformedClaims = await claimTransformLogic.TransformAsync(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            var validClaims = ValidateClaims(party, transformedClaims);
            logger.ScopeTrace(() => $"AuthMethod, SAML output SAML claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var jwtValidClaims = await claimsOAuthDownLogic.FromSamlToJwtClaimsAsync(validClaims);

            logger.ScopeTrace(() => $"AuthMethod, SAML output JWT claims '{jwtValidClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return jwtValidClaims;
        }

        private static Saml2Http.HttpRequest GetHttpRequest(string subjectToken)
        {
            return new Saml2Http.HttpRequest { Method = "DIRECT", Body = subjectToken };
        }
    }
}
