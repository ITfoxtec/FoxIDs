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
using System.Threading.Tasks;
using ITfoxtec.Identity.Saml2.Claims;
using System.Linq;
using Microsoft.IdentityModel.Tokens.Saml2;
using FoxIDs.Models.Sequences;

namespace FoxIDs.Logic
{
    public class SamlLogoutUpLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public SamlLogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, SecurityHeaderLogic securityHeaderLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> LogoutRequestRedirectAsync(UpPartyLink partyLink, LogoutRequest logoutRequest)
        {
            logger.ScopeTrace("Up, SAML Logout request.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await logoutRequest.ValidateObjectAsync();

            await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
            {
                DownPartyLink = logoutRequest.DownPartyLink,
                UpPartyId = partyId,
                SessionId = logoutRequest.SessionId,
                RequireLogoutConsent = logoutRequest.RequireLogoutConsent,
                PostLogoutRedirect = logoutRequest.PostLogoutRedirect,
                Claims = logoutRequest.Claims.ToClaimAndValues()
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.SamlUpJumpController, Constants.Endpoints.UpJump.LogoutRequest, includeSequence: true).ToRedirectResult();
        }

        public async Task<IActionResult> LogoutRequestAsync(string partyId)
        {
            logger.ScopeTrace("Up, SAML Logout request.");
            var samlUpSequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: false);
            if (!samlUpSequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid up-party id.");
            }
            logger.SetScopeProperty("upPartyId", samlUpSequenceData.UpPartyId);

            if (samlUpSequenceData.RequireLogoutConsent)
            {
                throw new NotSupportedException("Require SAML up logout consent not supported.");
            }
            if (!samlUpSequenceData.PostLogoutRedirect)
            {
                throw new NotSupportedException("SAML up post logout redirect required.");
            }

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);
            ValidatePartyLogoutSupport(party);

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

        private async Task<IActionResult> LogoutRequestAsync<T>(SamlUpParty party, Saml2Binding<T> binding, SamlUpSequenceData samlUpSequenceData)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party, includeSigningAndDecryptionCertificate: true);

            binding.RelayState = SequenceString;

            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig);

            var session = await sessionUpPartyLogic.GetSessionAsync(party);
            if (session == null)
            {
                return await LogoutResponseAsync(party);
            }

            try
            {
                if (!samlUpSequenceData.SessionId.Equals(session.SessionId, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match up-party session ID.");
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
            }

            saml2LogoutRequest.SessionIndex = session.ExternalSessionId;

            samlUpSequenceData.SessionDownPartyLinks = session.DownPartyLinks;
            samlUpSequenceData.SessionClaims = session.Claims;
            await sequenceLogic.SaveSequenceDataAsync(samlUpSequenceData);

            var jwtClaims = samlUpSequenceData.SessionClaims.ToClaimList();
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
            logger.ScopeTrace($"SAML Logout request '{saml2LogoutRequest.XmlDocument.OuterXml}'.");
            logger.ScopeTrace($"Logout URL '{samlConfig.SingleLogoutDestination?.OriginalString}'.");
            logger.ScopeTrace("Up, SAML Logout request.", triggerEvent: true);

            _ = await sessionUpPartyLogic.DeleteSessionAsync(session);
            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(samlUpSequenceData.SessionId);

            securityHeaderLogic.AddFormActionAllowAll();

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

        public async Task<IActionResult> LogoutResponseAsync(string partyId)
        {
            logger.ScopeTrace($"Up, SAML Logout response.");
            logger.SetScopeProperty("upPartyId", partyId);

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);
            ValidatePartyLogoutSupport(party);

            return await LogoutResponseAsync(party);
        }

        private async Task<IActionResult> LogoutResponseAsync(SamlUpParty party)
        {
            logger.ScopeTrace($"Binding '{party.LogoutBinding.ResponseBinding}'");
            switch (party.LogoutBinding.ResponseBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await LogoutResponseAsync(party, new Saml2RedirectBinding());
                case SamlBindingTypes.Post:
                    return await LogoutResponseAsync(party, new Saml2PostBinding());
                default:
                    throw new NotSupportedException($"SAML binding '{party.LogoutBinding.ResponseBinding}' not supported.");
            }
        }

        private async Task<IActionResult> LogoutResponseAsync<T>(SamlUpParty party, Saml2Binding<T> binding)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party);

            var saml2LogoutResponse = new Saml2LogoutResponse(samlConfig);

            binding.ReadSamlResponse(HttpContext.Request.ToGenericHttpRequest(), saml2LogoutResponse);

            await sequenceLogic.ValidateSequenceAsync(binding.RelayState);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: party.DisableSingleLogout);

            try
            {
                logger.ScopeTrace($"SAML Logout response '{saml2LogoutResponse.XmlDocument.OuterXml}'.");
                logger.SetScopeProperty("status", saml2LogoutResponse.Status.ToString());
                logger.ScopeTrace("Up, SAML Logout response.", triggerEvent: true);

                if (saml2LogoutResponse.Status != Saml2StatusCodes.Success)
                {
                    throw new SamlRequestException("Unsuccessful Logout response.") { RouteBinding = RouteBinding, Status = saml2LogoutResponse.Status };
                }

                binding.Unbind(HttpContext.Request.ToGenericHttpRequest(), saml2LogoutResponse);
                logger.ScopeTrace("Up, Successful SAML Logout response.", triggerEvent: true);

                if (party.DisableSingleLogout)
                {
                    return await LogoutResponseDownAsync(sequenceData);
                }
                else
                {
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutDownLogic.InitializeSingleLogoutAsync(new UpPartyLink { Name = party.Name, Type = party.Type }, sequenceData.DownPartyLink, sequenceData.SessionDownPartyLinks, sequenceData.SessionClaims);
                    if (doSingleLogout)
                    {
                        return await singleLogoutDownLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                    else
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<SamlUpSequenceData>();
                        return await LogoutResponseDownAsync(sequenceData);
                    }
                }
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

        public async Task<IActionResult> SingleLogoutDoneAsync(string partyId)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: true);
            if (!sequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid up-party id.");
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
                logger.ScopeTrace($"Response, Down type {sequenceData.DownPartyLink.Type}.");
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

        public async Task<IActionResult> SingleLogoutRequestAsync(string partyId)
        {
            logger.ScopeTrace("Up, SAML Single Logout request.");
            logger.SetScopeProperty("upPartyId", partyId);

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);
            ValidatePartyLogoutSupport(party);

            switch (party.LogoutBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await SingleLogoutRequestAsync(party, new Saml2RedirectBinding());
                case SamlBindingTypes.Post:
                    return await SingleLogoutRequestAsync(party, new Saml2PostBinding());
                default:
                    throw new NotSupportedException($"SAML binding '{party.LogoutBinding.RequestBinding}' not supported.");
            }
        }

        private async Task<IActionResult> SingleLogoutRequestAsync<T>(SamlUpParty party, Saml2Binding<T> binding)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party);
                        
            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig);

            binding.ReadSamlRequest(HttpContext.Request.ToGenericHttpRequest(), saml2LogoutRequest);

            try
            {
                logger.ScopeTrace($"SAML Single Logout request '{saml2LogoutRequest.XmlDocument.OuterXml}'.");
                logger.ScopeTrace("Up, SAML Single Logout request.", triggerEvent: true);

                binding.Unbind(HttpContext.Request.ToGenericHttpRequest(), saml2LogoutRequest);
                logger.ScopeTrace("Up, Successful SAML Single Logout request.", triggerEvent: true);

                var session = await sessionUpPartyLogic.DeleteSessionAsync();
                await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(session.SessionId);

                await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
                {
                    ExternalInitiatedSingleLogout = true,
                    Id = saml2LogoutRequest.IdAsString,
                    UpPartyId = party.Id,
                    RelayState = binding.RelayState,
                    SessionId = saml2LogoutRequest.SessionIndex
                });

                if (party.DisableSingleLogout)
                {
                    return await SingleLogoutResponseAsync(party, samlConfig, saml2LogoutRequest.Id.Value, binding.RelayState);
                }
                else
                {
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutDownLogic.InitializeSingleLogoutAsync(new UpPartyLink { Name = party.Name, Type = party.Type }, null, session.DownPartyLinks, session.Claims);

                    if (doSingleLogout)
                    {
                        return await singleLogoutDownLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                    else
                    {
                        return await SingleLogoutResponseAsync(party, samlConfig, saml2LogoutRequest.Id.Value, binding.RelayState);
                    }
                }

            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await SingleLogoutResponseAsync(party, samlConfig, saml2LogoutRequest.Id.Value, binding.RelayState, ex.Status);
            }
        }

        private string GetSingleLogoutResponseUrl(SamlUpParty party) => party.SingleLogoutResponseUrl.IsNullOrEmpty() ? party.LogoutUrl : party.SingleLogoutResponseUrl;

        private async Task<IActionResult> SingleLogoutResponseAsync(SamlUpSequenceData sequenceData, Saml2StatusCodes status = Saml2StatusCodes.Success, string sessionIndex = null)
        {
            logger.SetScopeProperty("upPartyId", sequenceData.UpPartyId);

            var party = await tenantRepository.GetAsync<SamlUpParty>(sequenceData.UpPartyId);
            ValidatePartyLogoutSupport(party);

            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party, includeSigningAndDecryptionCertificate: true);            
            return await SingleLogoutResponseAsync(party, samlConfig, sequenceData.Id, sequenceData.RelayState, status, sessionIndex);
        }

        private async Task<IActionResult> SingleLogoutResponseAsync(SamlUpParty party, Saml2Configuration samlConfig, string inResponseTo, string relayState, Saml2StatusCodes status = Saml2StatusCodes.Success, string sessionIndex = null)
        {
            logger.ScopeTrace($"Down, SAML Single Logout response{(status != Saml2StatusCodes.Success ? " error" : string.Empty)}, Status code '{status}'.");

            var binding = party.LogoutBinding.ResponseBinding;
            logger.ScopeTrace($"Binding '{binding}'");
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

        private async Task<IActionResult> LogoutResponseAsync<T>(Saml2Configuration samlConfig, string inResponseTo, string relayState, string singleLogoutResponseUrl, Saml2Binding<T> binding, Saml2StatusCodes status, string sessionIndex)
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
            logger.ScopeTrace($"SAML Single Logout response '{saml2LogoutResponse.XmlDocument.OuterXml}'.");
            logger.ScopeTrace($"Single logged out response URL '{singleLogoutResponseUrl}'.");
            logger.ScopeTrace("Down, SAML Single Logout response.", triggerEvent: true);

            await sequenceLogic.RemoveSequenceDataAsync<SamlDownSequenceData>();
            securityHeaderLogic.AddFormAction(singleLogoutResponseUrl);
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
    }
}
