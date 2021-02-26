using ITfoxtec.Identity;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class OpenIDConfigController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;

        public OpenIDConfigController(TelemetryScopedLogger logger, IServiceProvider serviceProvider) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task<IActionResult> OpenidConfiguration()
        {
            try
            {
                logger.ScopeTrace($"Openid configuration, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.OAuth2:
                        return Json(await serviceProvider.GetService<OidcDiscoveryDownLogic<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>>().OpenidConfiguration(RouteBinding.DownParty.Id), JsonExtensions.SettingsIndented);
                    case PartyTypes.Oidc:
                        return Json(await serviceProvider.GetService<OidcDiscoveryDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().OpenidConfiguration(RouteBinding.DownParty.Id), JsonExtensions.SettingsIndented);

                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Openid Configuration failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    
        public IActionResult Keys()
        {
            try
            {
                logger.ScopeTrace($"Openid configuration keys, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.OAuth2:
                        return Json(serviceProvider.GetService<OidcDiscoveryDownLogic<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>>().Keys(RouteBinding.DownParty.Id), JsonExtensions.SettingsIndented);
                    case PartyTypes.Oidc:
                        return Json(serviceProvider.GetService<OidcDiscoveryDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().Keys(RouteBinding.DownParty.Id), JsonExtensions.SettingsIndented);

                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Openid Configuration Keys failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
