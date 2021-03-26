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
                logger.ScopeTrace($"SAML SP Metadata request, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlMetadataLogic>().SpMetadataAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML SP Metadata request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> IdPMetadata()
        {
            try
            {
                logger.ScopeTrace($"SAML IdP Metadata request, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlMetadataLogic>().IdPMetadataAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML IdP Metadata request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> Acs()
        {
            try
            {
                logger.ScopeTrace($"SAML Authn response, Up type '{RouteBinding.UpParty.Type}'");
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
                var genericHttpRequest = Request.ToGenericHttpRequest();
                if (new Saml2PostBinding().IsResponse(genericHttpRequest) || new Saml2RedirectBinding().IsResponse(genericHttpRequest))
                {
                    return await LoggedOutInternal();
                }
                else
                {
                    await sequenceLogic.StartSequenceAsync(true);
                    return await SingleLogoutInternal();
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logged Out response or Single Logout request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LoggedOutInternal()
        {
            try
            {
                logger.ScopeTrace($"SAML Logged Out response, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutResponseAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logged Out response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> SingleLogoutInternal()
        {
            try
            {
                logger.ScopeTrace($"SAML Single Logout request, Up type '{RouteBinding.UpParty.Type}'");
                switch (RouteBinding.UpParty.Type)
                {
                    //case PartyTypes.Saml2:
                    //    return await serviceProvider.GetService<SamlLogoutUpLogic>().SingleLogoutResponseAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Single Logout request failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> Authn()
        {
            try
            {
                if (RouteBinding.ToUpParties?.Count() < 1)
                {
                    throw new NotSupportedException("Up-party not defined.");
                }
                if (RouteBinding.ToUpParties?.Count() != 1)
                {
                    throw new NotSupportedException("Currently only exactly 1 to up-party is supported.");
                }

                logger.ScopeTrace($"SAML Authn request, Down type '{RouteBinding.DownParty.Type}'");
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
                    throw new NotSupportedException("Up-party not defined.");
                }
                if (RouteBinding.ToUpParties?.Count() != 1)
                {
                    throw new NotSupportedException("Currently only exactly 1 to up-party is supported.");
                }

                var genericHttpRequest = Request.ToGenericHttpRequest();
                if (new Saml2PostBinding().IsRequest(genericHttpRequest) || new Saml2RedirectBinding().IsRequest(genericHttpRequest))
                {
                    await sequenceLogic.StartSequenceAsync(true);
                    return await LogoutInternal();
                }
                else
                {
                    return await SingleLogoutResponseInternal();
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logout request or Single Logout response failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LogoutInternal()
        {
            try
            {
                logger.ScopeTrace($"SAML Logout request, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutRequestAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{RouteBinding.DownParty.Type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logout request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> SingleLogoutResponseInternal()
        {
            try
            {
                logger.ScopeTrace($"SAML Single Logout response, Down type '{RouteBinding.DownParty.Type}'");
                switch (RouteBinding.DownParty.Type)
                {
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().SingleLogoutResponseAsync(RouteBinding.DownParty.Id);
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