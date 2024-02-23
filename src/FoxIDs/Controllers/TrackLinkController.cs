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
    
        public async Task<IActionResult> AuthResponse()
        {
            try
            {
                logger.ScopeTrace(() => $"Environment Link auth response, Authentication method name '{RouteBinding.UpParty.Name}'");
                return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthResponseAsync(RouteBinding.UpParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Environment Link auth response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence]
        public async Task<IActionResult> RpLogoutRequestJump()
        {
            try
            {
                logger.ScopeTrace(() => $"Environment Link RP initiated logout request, Authentication method name '{RouteBinding.UpParty.Name}'");
                return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutUpLogic>().LogoutRequestAsync(RouteBinding.UpParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Environment Link RP initiated logout request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> RpLogoutResponse()
        {
            try
            {
                logger.ScopeTrace(() => $"Environment Link RP initiated logout response, Authentication method name '{RouteBinding.UpParty.Name}'");
                return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutUpLogic>().LogoutResponseAsync(RouteBinding.UpParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Environment Link RP initiated logout response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> FrontChannelLogout()
        {
            try
            {
                logger.ScopeTrace(() => $"Environment Link front channel logout request, Authentication method name '{RouteBinding.UpParty.Name}'");
                return await serviceProvider.GetService<TrackLinkFrontChannelLogoutUpLogic>().FrontChannelLogoutAsync(RouteBinding.UpParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Environment Link front channel logout request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence]
        public async Task<IActionResult> FrontChannelLogoutDone()
        {
            try
            {
                logger.ScopeTrace(() => $"Environment Link front channel logout Done, Down type '{RouteBinding.DownParty.Name}'");
                return await serviceProvider.GetService<TrackLinkFrontChannelLogoutDownLogic>().LogoutDoneAsync();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Environment Link front channel logout Done failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence]
        public async Task<IActionResult> SingleLogoutDone()
        {
            try
            {
                logger.ScopeTrace(() => $"Environment Link single Logout done, Authentication method name '{RouteBinding.UpParty.Name}'");
                return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutUpLogic>().SingleLogoutDone(RouteBinding.UpParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Environment Link single Logout done failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> AuthRequest()
        {
            try
            {
                logger.ScopeTrace(() => $"Environment Link auth request, Application registration name '{RouteBinding.DownParty.Name}'");
                return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthRequestAsync(RouteBinding.DownParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Environment Link auth request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> RpLogoutRequest()
        {
            try
            {
                logger.ScopeTrace(() => $"Environment Link RP initiated logout request, Application registration name '{RouteBinding.DownParty.Name}'");
                return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutDownLogic>().LogoutRequestAsync(RouteBinding.DownParty.Id);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Environment Link RP initiated logout request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
