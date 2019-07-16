using System;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Controllers
{
    public class SamlController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;

        public SamlController(TelemetryScopedLogger logger, IServiceProvider serviceProvider) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task<IActionResult> SpMetadata()
        {
            try
            {
                var type = RouteBinding.UpParty.Type.ToEnum<PartyType>();
                logger.ScopeTrace($"SAML SP Metadata request, Up type '{type}'");
                switch (type)
                {
                    case PartyType.Saml2:
                        return await serviceProvider.GetService<SamlMetadataLogic>().SpMetadataAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
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
                var type = RouteBinding.DownParty.Type.ToEnum<PartyType>();
                logger.ScopeTrace($"SAML IdP Metadata request, Down type '{type}'");
                switch (type)
                {
                    case PartyType.Saml2:
                        return await serviceProvider.GetService<SamlMetadataLogic>().IdPMetadataAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
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
                var type = RouteBinding.UpParty.Type.ToEnum<PartyType>();
                logger.ScopeTrace($"SAML Authn response, Up type '{type}'");
                switch (type)
                {
                    case PartyType.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnResponseAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
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
                var type = RouteBinding.UpParty.Type.ToEnum<PartyType>();
                logger.ScopeTrace($"SAML Logout response, Up type '{type}'");
                switch (type)
                {
                    case PartyType.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutResponseAsync(RouteBinding.UpParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logout response failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> Authn()
        {
            try
            {
                if (RouteBinding.ToUpParties?.Count() < 1)
                {
                    throw new NotSupportedException("Up party not defined.");
                }
                if (RouteBinding.ToUpParties?.Count() != 1)
                {
                    throw new NotSupportedException("Currently only exactly 1 to up party is supported.");
                }

                var type = RouteBinding.DownParty.Type.ToEnum<PartyType>();
                logger.ScopeTrace($"SAML Authn request, Down type '{type}'");
                switch (type)
                {
                    case PartyType.Saml2:
                        return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnRequestAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Authn request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [Sequence(SequenceAction.Start)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                if (RouteBinding.ToUpParties?.Count() < 1)
                {
                    throw new NotSupportedException("Up party not defined.");
                }
                if (RouteBinding.ToUpParties?.Count() != 1)
                {
                    throw new NotSupportedException("Currently only exactly 1 to up party is supported.");
                }

                var type = RouteBinding.DownParty.Type.ToEnum<PartyType>();
                logger.ScopeTrace($"SAML Logout request, Down type '{type}'");
                switch (type)
                {
                    case PartyType.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutRequestAsync(RouteBinding.DownParty.Id);
                    default:
                        throw new NotSupportedException($"Party type '{type}' not supported.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SAML Logout request failed, Name '{RouteBinding.DownParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}