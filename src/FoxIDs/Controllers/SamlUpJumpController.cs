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
    public class SamlUpJumpController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;

        public SamlUpJumpController(TelemetryScopedLogger logger, IServiceProvider serviceProvider) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task<IActionResult> AuthnRequest()
        {
            try
            {
                logger.ScopeTrace(() => $"Authn request, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Authn request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> LogoutRequest()
        {
            try
            {
                logger.ScopeTrace(() => $"Logout request, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutRequestAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Logout request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}