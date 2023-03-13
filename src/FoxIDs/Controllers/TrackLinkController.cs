using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class TrackLinkController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;

        public TrackLinkController(TelemetryScopedLogger logger, IServiceProvider serviceProvider) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> AuthRequest()
        {
            try
            {
                logger.ScopeTrace(() => $"Track link auth request, Down party name '{RouteBinding.DownParty.Name}'");
                return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthRequestAsync(RouteBinding.DownParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Track link auth request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> AuthResponse()
        {
            try
            {
                logger.ScopeTrace(() => $"Track link auth response, Up party name '{RouteBinding.UpParty.Name}'");
                return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthResponseAsync(RouteBinding.UpParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Track link auth response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
