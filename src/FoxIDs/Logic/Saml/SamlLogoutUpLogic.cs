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
        private readonly FormActionLogic formActionLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;
        private readonly OAuthRefreshTokenGrantLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public SamlLogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, FormActionLogic formActionLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, OAuthRefreshTokenGrantLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.formActionLogic = formActionLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> LogoutAsync(UpPartyLink partyLink, LogoutRequest logoutRequest)
        {
            logger.ScopeTrace("Up, SAML Logout request.");
            var partyId = await UpParty.IdFormat(RouteBinding, partyLink.Name);
            logger.SetScopeProperty("upPartyId", partyId);

            await logoutRequest.ValidateObjectAsync();

            await sequenceLogic.SaveSequenceDataAsync(new SamlUpSequenceData
            {
                DownPartyId = logoutRequest.DownParty.Id,
                DownPartyType = logoutRequest.DownParty.Type,
            });

            if (logoutRequest.RequireLogoutConsent)
            {
                throw new NotSupportedException("Require SAML up logout consent not supported.");
            }
            if (!logoutRequest.PostLogoutRedirect)
            {
                throw new NotSupportedException("SAML up post logout redirect required.");
            }

            var party = await tenantRepository.GetAsync<SamlUpParty>(partyId);
            ValidatePartyLogoutSupport(party);

            switch (party.LogoutBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await LogoutAsync(party, new Saml2RedirectBinding(), logoutRequest);
                case SamlBindingTypes.Post:
                    return await LogoutAsync(party, new Saml2PostBinding(), logoutRequest);
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

        private async Task<IActionResult> LogoutAsync<T>(SamlUpParty party, Saml2Binding<T> binding, LogoutRequest logoutRequest)
        {
            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party, includeSigningCertificate: true);

            binding.RelayState = SequenceString;

            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig);
            saml2LogoutRequest.SessionIndex = logoutRequest.SessionId;

            var nameID = logoutRequest.Claims?.Where(c => c.Type == Saml2ClaimTypes.NameId).Select(c => c.Value).FirstOrDefault();
            var nameIdFormat = logoutRequest.Claims?.Where(c => c.Type == Saml2ClaimTypes.NameIdFormat).Select(c => c.Value).FirstOrDefault();
            if (!nameID.IsNullOrEmpty())
            {
                var prePartyName = $"{party.Name}|";
                if(prePartyName.StartsWith(prePartyName, StringComparison.Ordinal))
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
            logger.ScopeTrace($"Logout url '{samlConfig.SingleLogoutDestination?.OriginalString}'.");
            logger.ScopeTrace("Up, SAML Logout request.", triggerEvent: true);

            await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(logoutRequest.SessionId);

            if (binding is Saml2Binding<Saml2RedirectBinding>)
            {
                return await Task.FromResult((binding as Saml2RedirectBinding).ToActionResult());
            }
            if (binding is Saml2Binding<Saml2PostBinding>)
            {
                await formActionLogic.AddFormActionByUrlAsync(samlConfig.SingleLogoutDestination.OriginalString);
                return await Task.FromResult((binding as Saml2PostBinding).ToActionResult());
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
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlUpSequenceData>();

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

                return await LogoutResponseDownAsync(sequenceData, saml2LogoutResponse.Status, saml2LogoutResponse.SessionIndex);
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
            logger.ScopeTrace($"Response, Down type {sequenceData.DownPartyType}.");
            switch (sequenceData.DownPartyType)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    if(status == Saml2StatusCodes.Success)
                    {
                        return await serviceProvider.GetService<OidcEndSessionDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().EndSessionResponseAsync(sequenceData.DownPartyId);
                    }
                    else
                    {
                        throw new EndpointException($"SAML up Logout failed, Status '{status}', Name '{RouteBinding.UpParty.Name}'.") { RouteBinding = RouteBinding };
                    }
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyId, status, sessionIndex);

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
