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
        public async Task<IActionResult> LinkRequest()
        {
            try
            {
                logger.ScopeTrace(() => $"Track link request, Down party name '{RouteBinding.DownParty.Name}'");
                return await serviceProvider.GetService<TrackLinkDownLogic>().LinkRequestAsync(RouteBinding.DownParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Track link request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> LinkResponse()
        {
            try
            {
                logger.ScopeTrace(() => $"Track link response, Up party name '{RouteBinding.UpParty.Name}'");
                return await serviceProvider.GetService<TrackLinkUpLogic>().LinkResponseAsync(RouteBinding.UpParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Track link response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
