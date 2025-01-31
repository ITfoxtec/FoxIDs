using Saml2Http = ITfoxtec.Identity.Saml2.Http;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.IdentityModel.Tokens.Saml2;
using FoxIDs.Models.Sequences;
using FoxIDs.Logic.Tracks;

namespace FoxIDs.Logic
{
    public class SamlLogoutUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public SamlLogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, HrdLogic hrdLogic, SessionUpPartyLogic sessionUpPartyLogic, SecurityHeaderLogic securityHeaderLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, SingleLogoutLogic singleLogoutLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
            this.singleLogoutLogic = singleLogoutLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> LogoutRequestRedirectAsync(UpPartyLink partyLink, LogoutRequest logoutRequest, bool isSingleLogout = false)
        {
            logger.ScopeTrace(() => "AuthMethod, SAML Logout request.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await logoutRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
            {
                IsSingleLogout = isSingleLogout,
                DownPartyLink = logoutRequest?.DownPartyLink,
                UpPartyId = partyId,
                SessionId = logoutRequest?.SessionId,
                RequireLogoutConsent = logoutRequest?.RequireLogoutConsent ?? false,
                PostLogoutRedirect = logoutRequest?.PostLogoutRedirect ?? false
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.SamlUpJumpController, Constants.Endpoints.UpJump.LogoutRequest, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult();
        }

        public async Task<IActionResult> LogoutRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, SAML Logout request.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            var samlUpSequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: false);
            if (!samlUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }

            if (samlUpSequenceData.RequireLogoutConsent)
            {
                throw new NotSupportedException("Require SAML up logout consent not supported.");
            }
            if (!samlUpSequenceData.PostLogoutRedirect)
            {
                throw new NotSupportedException("SAML up post logout redirect required.");
            }

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(partyId);

            try
            {
                ValidatePartyLogoutSupport(party);
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
                return await LogoutResponseInternalAsync(party, samlUpSequenceData);
            }

            switch (party.LogoutBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await LogoutRequestAsync(party, new Saml2RedirectBinding(), samlUpSequenceData);
                case SamlBindingTypes.Post:
                    return await LogoutRequestAsync(party, new Saml2PostBinding(), samlUpSequenceData);
                default:
                    throw new NotSupportedException($"SAML binding '{party.LogoutBinding.RequestBinding}' not supported.");
            }
        }

        private void ValidatePartyLogoutSupport(SamlUpParty party)
        {
            if (party.LogoutBinding == null || party.LogoutUrl.IsNullOrEmpty())
            {
                throw new EndpointException("Logout not configured.") { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LogoutRequestAsync(SamlUpParty party, Saml2Binding binding, SamlUpSequenceData sequenceData)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSigningAndDecryptionCertificate: true);

            binding.RelayState = await sequenceLogic.CreateExternalSequenceIdAsync();

            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig);

            var session = await sessionUpPartyLogic.GetSessionAsync(party);
            if (session == null)
            {
                if (sequenceData.IsSingleLogout)
                {
                    return await singleLogoutLogic.HandleSingleLogoutUpAsync();
                }
                else
                {
                    return await LogoutResponseDownAsync(sequenceData);
                }
            }

            try
            {
                if (!sequenceData.SessionId.IsNullOrEmpty() && !sequenceData.SessionId.Equals(session.SessionIdClaim, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match authentication method session ID.");
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
            }

            saml2LogoutRequest.SessionIndex = session.ExternalSessionId;

            var jwtClaims = session.Claims.ToClaimList();
            var nameID = jwtClaims?.Where(c => c.Type == JwtClaimTypes.Subject).Select(c => c.Value).FirstOrDefault();
            var nameIdFormat = jwtClaims?.Where(c => c.Type == Constants.JwtClaimTypes.SubFormat).Select(c => c.Value).FirstOrDefault();
            if (!nameID.IsNullOrEmpty())
            {
                var prePartyName = $"{party.Name}|";
                if(nameID.StartsWith(prePartyName, StringComparison.Ordinal))
                {
                    nameID = nameID.Remove(0, prePartyName.Length);
                }
                if (nameIdFormat.IsNullOrEmpty())
                {
                    saml2LogoutRequest.NameId = new Saml2NameIdentifier(nameID);
                }
                else
                {
                    saml2LogoutRequest.NameId = new Saml2NameIdentifier(nameID, new Uri(nameIdFormat));
                }
            }

            binding.Bind(saml2LogoutRequest);
            logger.ScopeTrace(() => $"SAML Logout request '{saml2LogoutRequest.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => $"Logout URL '{samlConfig.SingleLogoutDestination?.OriginalString}'.");
            logger.ScopeTrace(() => "AuthMethod, SAML Logout request.", triggerEvent: true);

            _ = await sessionUpPartyLogic.DeleteSessionAsync(party, session);
            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsBySessionIdAsync(sequenceData.SessionId);

            securityHeaderLogic.AddFormActionAllowAll();

            return binding.ToSamlActionResult();
        }

        public async Task<IActionResult> LogoutResponseAsync(string partyId, Saml2Http.HttpRequest samlHttpRequest)
        {
            logger.ScopeTrace(() => $"AuthMethod, SAML Logout response.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(partyId);
            ValidatePartyLogoutSupport(party);

            if (samlHttpRequest.Binding is Saml2RedirectBinding || samlHttpRequest.Binding is Saml2PostBinding)
            {
                logger.ScopeTrace(() => $"Binding, configured '{party.LogoutBinding.ResponseBinding}', actual '{samlHttpRequest.Binding.GetType().Name}'");
                return await LogoutResponseAsync(party, samlHttpRequest);
            }
            else
            {
                throw new NotSupportedException($"Binding '{samlHttpRequest.Binding.GetType().Name}' not supported.");
            }
        }

        private async Task<IActionResult> LogoutResponseAsync(SamlUpParty party, Saml2Http.HttpRequest samlHttpRequest)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party);

            var saml2LogoutResponse = new Saml2LogoutResponse(samlConfig);
            samlHttpRequest.Binding.ReadSamlResponse(samlHttpRequest, saml2LogoutResponse);

            await sequenceLogic.ValidateExternalSequenceIdAsync(samlHttpRequest.Binding.RelayState);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: party.DisableSingleLogout);

            try
            {
                logger.ScopeTrace(() => $"SAML Logout response '{saml2LogoutResponse.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
                logger.SetScopeProperty(Constants.Logs.Status, saml2LogoutResponse.Status.ToString());
                logger.ScopeTrace(() => "AuthMethod, SAML Logout response.", triggerEvent: true);

                if (saml2LogoutResponse.Status != Saml2StatusCodes.Success)
                {
                    throw new SamlRequestException("Unsuccessful Logout response.") { RouteBinding = RouteBinding, Status = saml2LogoutResponse.Status };
                }

                try
                {
                    samlHttpRequest.Binding.Unbind(samlHttpRequest, saml2LogoutResponse);
                    logger.ScopeTrace(() => "AuthMethod, Successful SAML Logout response.", triggerEvent: true);

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

                return await LogoutResponseInternalAsync(party, sequenceData);
            }
            catch (StopSequenceException)
            {
                throw;
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await LogoutResponseDownAsync(sequenceData, status: ex.Status);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return await LogoutResponseDownAsync(sequenceData, status: Saml2StatusCodes.Responder);
            }
        }

        private async Task<IActionResult> LogoutResponseInternalAsync(SamlUpParty party, SamlUpSequenceData sequenceData)
        {
            if (sequenceData.IsSingleLogout)
            {
                return await singleLogoutLogic.HandleSingleLogoutUpAsync();
            }
            else
            {
                if (party.DisableSingleLogout)
                {
                    await sessionUpPartyLogic.DeleteSessionTrackCookieGroupAsync(party);
                    return await LogoutResponseDownAsync(sequenceData);
                }
                else
                {
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutLogic.InitializeSingleLogoutAsync(party, sequenceData.DownPartyLink, sequenceData);
                    if (doSingleLogout)
                    {
                        return await singleLogoutLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                    else
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<SamlUpSequenceData>();
                        return await LogoutResponseDownAsync(sequenceData);
                    }
                }
            }
        }

        public async Task<IActionResult> SingleLogoutDoneAsync(string partyId)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: true);
            if (!sequenceData.IsSingleLogout && !sequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }
            if (!sequenceData.ExternalInitiatedSingleLogout)
            {
                return await LogoutResponseDownAsync(sequenceData);
            }
            else
            {
                return await SingleLogoutResponseAsync(sequenceData);
            }
        }

        private async Task<IActionResult> LogoutResponseDownAsync(SamlUpSequenceData sequenceData, Saml2StatusCodes status = Saml2StatusCodes.Success)
        {
            try
            {
                logger.ScopeTrace(() => $"Response, Application type {sequenceData.DownPartyLink.Type}.");
                switch (sequenceData.DownPartyLink.Type)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        if (status == Saml2StatusCodes.Success)
                        {
                            return await serviceProvider.GetService<OidcRpInitiatedLogoutDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().EndSessionResponseAsync(sequenceData.DownPartyLink.Id);
                        }
                        else
                        {
                            throw new StopSequenceException($"SAML up Logout failed, Status '{status}', Name '{RouteBinding.UpParty.Name}'.");
                        }
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyLink.Id, status, sequenceData.SessionId);
                    case PartyTypes.TrackLink:
                        if (status == Saml2StatusCodes.Success)
                        {
                            return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyLink.Id);
                        }
                        else
                        {
                            throw new StopSequenceException($"SAML up Logout failed, Status '{status}', Name '{RouteBinding.UpParty.Name}'.");
                        }

                    default:
                        throw new NotSupportedException();
                }
            }
            catch (StopSequenceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new StopSequenceException("Falling logout response down", ex);
            }
        }

        public async Task<IActionResult> SingleLogoutRequestAsync(string partyId, Saml2Http.HttpRequest samlHttpRequest)
        {
            logger.ScopeTrace(() => "AuthMethod, SAML Single Logout request.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(partyId);
            ValidatePartyLogoutSupport(party);

            if (samlHttpRequest.Binding is Saml2RedirectBinding || samlHttpRequest.Binding is Saml2PostBinding)
            {
                logger.ScopeTrace(() => $"Binding, configured '{party.LogoutBinding.RequestBinding}', actual '{samlHttpRequest.Binding.GetType().Name}'");
                return await SingleLogoutRequestAsync(party, samlHttpRequest);
            }
            else
            {
                throw new NotSupportedException($"Binding '{samlHttpRequest.Binding.GetType().Name}' not supported.");
            }
        }

        private async Task<IActionResult> SingleLogoutRequestAsync(SamlUpParty party, Saml2Http.HttpRequest samlHttpRequest)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party);
                        
            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig);
            samlHttpRequest.Binding.ReadSamlRequest(samlHttpRequest, saml2LogoutRequest);

            try
            {
                logger.ScopeTrace(() => $"SAML Single Logout request '{saml2LogoutRequest.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => "AuthMethod, SAML Single Logout request.", triggerEvent: true);

                try
                {
                    samlHttpRequest.Binding.Unbind(samlHttpRequest, saml2LogoutRequest);
                    logger.ScopeTrace(() => "AuthMethod, Successful SAML Single Logout request.", triggerEvent: true);

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

                var sequenceData = await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
                {
                    ExternalInitiatedSingleLogout = true,
                    Id = saml2LogoutRequest.IdAsString,
                    UpPartyId = party.Id,
                    RelayState = samlHttpRequest.Binding.RelayState,
                    SessionId = saml2LogoutRequest.SessionIndex
                });

                if (samlHttpRequest.Binding is Saml2PostBinding)
                {
                    return HttpContext.GetUpPartyUrl(party.Name, Constants.Routes.SamlController, Constants.Endpoints.UpJump.SingleLogoutRequestJump, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult();
                }
                else
                {
                    return await SingleLogoutRequestAsync(party, sequenceData);
                }
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await SingleLogoutResponseAsync(party, samlConfig, saml2LogoutRequest.Id.Value, samlHttpRequest.Binding.RelayState, ex.Status);
            }
        }

        private async Task<IActionResult> SingleLogoutRequestAsync(SamlUpParty party, SamlUpSequenceData sequenceData)
        {
            await hrdLogic.DeleteHrdSelectionBySelectedUpPartyAsync(party.Name);

            var session = await sessionUpPartyLogic.DeleteSessionAsync(party);
            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsBySessionIdAsync(session?.SessionIdClaim);

            if (party.DisableSingleLogout)
            {
                await sessionUpPartyLogic.DeleteSessionTrackCookieGroupAsync(party);
                var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSigningAndDecryptionCertificate: true);
                return await SingleLogoutResponseAsync(party, samlConfig, sequenceData.Id, sequenceData.RelayState);
            }
            else
            {
                (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutLogic.InitializeSingleLogoutAsync(party, null, sequenceData);
                if (doSingleLogout)
                {
                    return await singleLogoutLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                }
                else
                {
                    var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSigningAndDecryptionCertificate: true);
                    return await SingleLogoutResponseAsync(party, samlConfig, sequenceData.Id, sequenceData.RelayState);
                }
            }
        }

        public async Task<IActionResult> SingleLogoutRequestJumpAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, SAML Single Logout request jump.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: false);
            if (!sequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(partyId);
            return await SingleLogoutRequestAsync(party, sequenceData);
        }

        private string GetSingleLogoutResponseUrl(SamlUpParty party) => party.SingleLogoutResponseUrl.IsNullOrEmpty() ? party.LogoutUrl : party.SingleLogoutResponseUrl;

        private async Task<IActionResult> SingleLogoutResponseAsync(SamlUpSequenceData sequenceData, Saml2StatusCodes status = Saml2StatusCodes.Success, string sessionIndex = null)
        {
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            var party = await tenantDataRepository.GetAsync<SamlUpParty>(sequenceData.UpPartyId);
            ValidatePartyLogoutSupport(party);

            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSigningAndDecryptionCertificate: true);            
            return await SingleLogoutResponseAsync(party, samlConfig, sequenceData.Id, sequenceData.RelayState, status, sessionIndex);
        }

        private async Task<IActionResult> SingleLogoutResponseAsync(SamlUpParty party, Saml2Configuration samlConfig, string inResponseTo, string relayState, Saml2StatusCodes status = Saml2StatusCodes.Success, string sessionIndex = null)
        {
            logger.ScopeTrace(() => $"AppReg, SAML Single Logout response{(status != Saml2StatusCodes.Success ? " error" : string.Empty)}, Status code '{status}'.");

            var binding = party.LogoutBinding.ResponseBinding;
            logger.ScopeTrace(() => $"Binding '{binding}'");
            switch (binding)
            {
                case SamlBindingTypes.Redirect:
                    return await LogoutResponseAsync(samlConfig, inResponseTo, relayState, GetSingleLogoutResponseUrl(party), new Saml2RedirectBinding(), status, sessionIndex);
                case SamlBindingTypes.Post:
                    return await LogoutResponseAsync(samlConfig, inResponseTo, relayState, GetSingleLogoutResponseUrl(party), new Saml2PostBinding(), status, sessionIndex);
                default:
                    throw new NotSupportedException($"SAML binding '{binding}' not supported.");
            }
        }

        private async Task<IActionResult> LogoutResponseAsync(Saml2Configuration samlConfig, string inResponseTo, string relayState, string singleLogoutResponseUrl, Saml2Binding binding, Saml2StatusCodes status, string sessionIndex)
        {
            binding.RelayState = relayState;

            var saml2LogoutResponse = new Saml2LogoutResponse(samlConfig)
            {
                InResponseTo = new Saml2Id(inResponseTo),
                Status = status,
                Destination = new Uri(singleLogoutResponseUrl),
                SessionIndex = sessionIndex
            };

            binding.Bind(saml2LogoutResponse);
            logger.ScopeTrace(() => $"SAML Single Logout response '{saml2LogoutResponse.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => $"Single logged out response URL '{singleLogoutResponseUrl}'.");
            logger.ScopeTrace(() => "AppReg, SAML Single Logout response.", triggerEvent: true);

            await sequenceLogic.RemoveSequenceDataAsync<SamlDownSequenceData>();
            securityHeaderLogic.AddFormActionAllowAll();

            return binding.ToSamlActionResult();
        }
    }
}
