﻿using ITfoxtec.Identity;
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
                logger.ScopeTrace(() => $"OpenID configuration, Application type '{RouteBinding.DownParty?.Type}'");
                switch (RouteBinding.DownParty?.Type)
                {
                    case PartyTypes.OAuth2:
                        return Json(await serviceProvider.GetService<OidcDiscoveryExposeDownLogic<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>>().OpenidConfiguration(RouteBinding.DownParty?.Id), JsonExtensions.SettingsIndented);
                    case PartyTypes.Oidc:
                    case null:
                        return Json(await serviceProvider.GetService<OidcDiscoveryExposeDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().OpenidConfiguration(RouteBinding.DownParty?.Id), JsonExtensions.SettingsIndented);

                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty?.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"OpenID Configuration failed for client id '{RouteBinding.DownParty?.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    
        public IActionResult Keys()
        {
            try
            {
                logger.ScopeTrace(() => $"OpenID configuration keys, Application type '{RouteBinding.DownParty?.Type}'");
                switch (RouteBinding.DownParty?.Type)
                {
                    case PartyTypes.OAuth2:
                        return Json(serviceProvider.GetService<OidcDiscoveryExposeDownLogic<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>>().Keys(RouteBinding.DownParty?.Id), JsonExtensions.SettingsIndented);
                    case PartyTypes.Oidc:
                    case null:
                        return Json(serviceProvider.GetService<OidcDiscoveryExposeDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().Keys(RouteBinding.DownParty?.Id), JsonExtensions.SettingsIndented);

                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty?.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"OpenID Configuration Keys failed for client id '{RouteBinding.DownParty?.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
