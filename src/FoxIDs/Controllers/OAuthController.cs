using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using FoxIDs.Models.Sequences;
using FoxIDs.Infrastructure.Filters;

namespace FoxIDs.Controllers
{
    public class OAuthController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;

        public OAuthController(TelemetryScopedLogger logger, IServiceProvider serviceProvider) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task<IActionResult> AuthorizationResponse()
        {
            try
            {
                logger.ScopeTrace(() => $"Authorization response, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationResponseAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Authorization response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }


        public async Task<IActionResult> EndSessionResponse()
        {
            try
            {
                logger.ScopeTrace(() => $"End session response, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcRpInitiatedLogoutUpLogic<OidcUpParty, OidcUpClient>>().EndSessionResponseAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"End session response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> FrontChannelLogout()
        {
            try
            {
                logger.ScopeTrace(() => $"Front channel logout, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcFrontChannelLogoutUpLogic<OidcUpParty, OidcUpClient>>().FrontChannelLogoutAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Front channel logout failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence]
        public async Task<IActionResult> SingleLogoutDone()
        {
            try
            {
                logger.ScopeTrace(() => $"Single Logout done, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcRpInitiatedLogoutUpLogic<OidcUpParty, OidcUpClient>>().SingleLogoutDone(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Single Logout done failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> Authorize()
        {
            try
            {
                if (RouteBinding.ToUpParties?.Count() < 1)
                {
                    throw new NotSupportedException("Up-party not configured.");
                }

                logger.ScopeTrace(() => $"Authorize request, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationRequestAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Authorize request failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> Token()
        {
            try
            {
                logger.ScopeTrace(() => $"Token request, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.OAuth2:
                        return await serviceProvider.GetService<OAuthTokenDownLogic<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>>().TokenRequestAsync(RouteBinding.DownParty.Id);
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcTokenDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().TokenRequestAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Token request failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [OAuthBearerTokenUsageException]
        public async Task<IActionResult> UserInfo()
        {
            try
            {
                logger.ScopeTrace(() => $"UserInfo request, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.OAuth2:
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcUserInfoDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().UserInfoRequestAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"UserInfo request failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> EndSession()
        {
            try
            {
                if (RouteBinding.ToUpParties?.Count() < 1)
                {
                    throw new NotSupportedException("Up-party not configured.");
                }

                logger.ScopeTrace(() => $"End session, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcRpInitiatedLogoutDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().EndSessionRequestAsync(RouteBinding.DownParty.Id);

                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }

            }
            catch (Exception ex)
            {
                throw new EndpointException($"End Session request failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence]
        public async Task<IActionResult> FrontChannelLogoutDone()
        {
            try
            {
                logger.ScopeTrace(() => $"Front Channel Logout Done, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcFrontChannelLogoutDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().LogoutDoneAsync();

                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Front Channel Logout Done failed for client id '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }       
    }
}