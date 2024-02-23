using System;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using Saml2Http = ITfoxtec.Identity.Saml2.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Controllers
{
    public class SamlController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceLogic sequenceLogic;

        public SamlController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, SequenceLogic sequenceLogic) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
        }

        public async Task<IActionResult> SpMetadata()
        {
            try
            {
                logger.ScopeTrace(() => $"SAML SP Metadata request, Authentication type '{RouteBinding.UpParty?.Type}'");
                switch (RouteBinding.UpParty?.Type)
                {
                    case PartyTypes.Saml2:
                    case null:
                        return await serviceProvider.GetService<SamlMetadataExposeLogic>().SpMetadataAsync(RouteBinding.UpParty?.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty?.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML SP Metadata request failed, Name '{RouteBinding.UpParty?.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> IdPMetadata()
        {
            try
            {
                logger.ScopeTrace(() => $"SAML IdP Metadata request, Application type '{RouteBinding.DownParty?.Type}'");
                switch (RouteBinding.DownParty?.Type)
                {
                    case PartyTypes.Saml2:
                    case null:
                        return await serviceProvider.GetService<SamlMetadataExposeLogic>().IdPMetadataAsync(RouteBinding.DownParty?.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty?.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML IdP Metadata request failed, Name '{RouteBinding.DownParty?.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> Acs()
        {
            try
            {
                logger.ScopeTrace(() => $"SAML Authn response, Authentication type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnResponseAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Authn response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> SingleLogout()
        {
            try
            {
                var samlHttpRequest = Request.ToGenericHttpRequest(validate: true);
                if (samlHttpRequest.Binding is Saml2RedirectBinding || samlHttpRequest.Binding is Saml2PostBinding)
                {
                    if (samlHttpRequest.Binding.IsResponse(samlHttpRequest))
                    {
                        return await LoggedOutInternal(samlHttpRequest);
                    }
                    else
                    {
                        await sequenceLogic.StartSequenceAsync(true);
                        return await SingleLogoutInternal(samlHttpRequest);
                    }
                }
                else
                {
                    throw new NotSupportedException($"Binding '{samlHttpRequest.Binding.GetType().Name}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logged Out response or Single Logout request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LoggedOutInternal(Saml2Http.HttpRequest samlHttpRequest)
        {
            try
            {
                logger.ScopeTrace(() => $"SAML Logged Out response, Authentication type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutResponseAsync(RouteBinding.UpParty.Id, samlHttpRequest);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logged Out response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> SingleLogoutInternal(Saml2Http.HttpRequest samlHttpRequest)
        {
            try
            {
                logger.ScopeTrace(() => $"SAML Single Logout request, Authentication type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().SingleLogoutRequestAsync(RouteBinding.UpParty.Id, samlHttpRequest);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Single Logout request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence]
        public async Task<IActionResult> SingleLogoutRequestJump()
        {
            try
            {
                logger.ScopeTrace(() => $"SAML Single Logout request jump, Authentication type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().SingleLogoutRequestJumpAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Single Logout request jump failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence]
        public async Task<IActionResult> SingleLogoutDone()
        {
            try
            {
                logger.ScopeTrace(() => $"SAML Single Logout done, Authentication type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().SingleLogoutDoneAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Single Logout done failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> Authn()
        {
            try
            {
                if (RouteBinding.ToUpParties?.Count() < 1)
                {
                    throw new NotSupportedException("Authentication method not configured.");
                }

                logger.ScopeTrace(() => $"SAML Authn request, Application type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnRequestAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Authn request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> Logout()
        {
            try
            {
                if (RouteBinding.ToUpParties?.Count() < 1)
                {
                    throw new NotSupportedException("Authentication method not configured.");
                }

                var samlHttpRequest = Request.ToGenericHttpRequest(validate: true);
                if (samlHttpRequest.Binding is Saml2RedirectBinding || samlHttpRequest.Binding is Saml2PostBinding)
                {
                    if (samlHttpRequest.Binding.IsRequest(samlHttpRequest))
                    {
                        await sequenceLogic.StartSequenceAsync(true);
                        return await LogoutInternal(samlHttpRequest);
                    }
                    else
                    {
                        return await SingleLogoutResponseInternal(samlHttpRequest);
                    }
                }
                else
                {
                    throw new NotSupportedException($"Binding '{samlHttpRequest.Binding.GetType().Name}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logout request or Single Logout response failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LogoutInternal(Saml2Http.HttpRequest samlHttpRequest)
        {
            try
            {
                logger.ScopeTrace(() => $"SAML Logout request, Application type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutRequestAsync(RouteBinding.DownParty.Id, samlHttpRequest);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logout request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> SingleLogoutResponseInternal(Saml2Http.HttpRequest samlHttpRequest)
        {
            try
            {
                logger.ScopeTrace(() => $"SAML Single Logout response, Application type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().SingleLogoutResponseAsync(RouteBinding.DownParty.Id, samlHttpRequest);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Single Logout response failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }    
    }
}