using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Models.Sequences;
using FoxIDs.Infrastructure.Filters;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class OAuthUpJumpController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;

        public OAuthUpJumpController(TelemetryScopedLogger logger, IServiceProvider serviceProvider) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task<IActionResult> AuthorizationRequest()
        {
            try
            {
                logger.ScopeTrace(() => $"Authorization request, Authentication type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Connection type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Authorization request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> EndSessionRequest()
        {
            try
            {
                logger.ScopeTrace(() => $"End session request, Authentication type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcRpInitiatedLogoutUpLogic<OidcUpParty, OidcUpClient>>().EndSessionRequestAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Connection type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"End session request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}