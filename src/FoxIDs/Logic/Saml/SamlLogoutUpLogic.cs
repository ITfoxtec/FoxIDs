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
        private readonly FormActionLogic formActionLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public SamlLogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, FormActionLogic formActionLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.formActionLogic = formActionLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
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
                DownPartyId = logoutRequest.DownParty.Id,
                DownPartyType = logoutRequest.DownParty.Type,
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
                    throw new NotSupportedException($"Binding '{party.LogoutBinding.RequestBinding}' not supported.");
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
            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party, includeSigningCertificate: true);

            binding.RelayState = SequenceString;

            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig);

            var session = await sessionUpPartyLogic.GetSessionAsync(party);
            if (session != null)
            {
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

                saml2LogoutRequest.SessionIndex = samlUpSequenceData.SessionId;
            }

            var claims = samlUpSequenceData.Claims.ToClaimList();
            var nameID = claims?.Where(c => c.Type == Saml2ClaimTypes.NameId).Select(c => c.Value).FirstOrDefault();
            var nameIdFormat = claims?.Where(c => c.Type == Saml2ClaimTypes.NameIdFormat).Select(c => c.Value).FirstOrDefault();
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

            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(samlUpSequenceData.SessionId);

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

        public async Task<IActionResult> LogoutResponseAsync(string partyId)
        {
            logger.ScopeTrace($"Up, SAML Logout response.");
            logger.SetScopeProperty("upPartyId", partyId);

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);
            ValidatePartyLogoutSupport(party);

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
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>(remove: true);

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

                await sessionUpPartyLogic.DeleteSessionAsync();

                return await LogoutResponseDownAsync(sequenceData, saml2LogoutResponse.Status, saml2LogoutResponse.SessionIndex);
            }
            catch (StopSequenceException)
            {
                throw;
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await LogoutResponseDownAsync(sequenceData, ex.Status);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return await LogoutResponseDownAsync(sequenceData, Saml2StatusCodes.Responder);
            }
        }

        private async Task<IActionResult> LogoutResponseDownAsync(SamlUpSequenceData sequenceData, Saml2StatusCodes status, string sessionIndex = null)
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
                            return await serviceProvider.GetService<OidcEndSessionDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().EndSessionResponseAsync(sequenceData.DownPartyId);
                        }
                        else
                        {
                            throw new StopSequenceException($"SAML up Logout failed, Status '{status}', Name '{RouteBinding.UpParty.Name}'.");
                        }
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyId, status, sessionIndex);

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
    }
}
