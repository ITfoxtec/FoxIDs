using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UrlCombineLib;
using FoxIDs.Models.Config;

namespace FoxIDs.Logic
{
    public class SamlMetadataLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;

        public SamlMetadataLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
        }

        public async Task<IActionResult> SpMetadataAsync(string partyId)
        {
            logger.ScopeTrace(() => "Up, SP Metadata request.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);            
            var party = RouteBinding.DownParty != null ? await tenantRepository.GetAsync<SamlUpParty>(partyId) : null;

            var samlConfig = saml2ConfigurationLogic.GetSamlUpConfig(party, true);
           
            var acsDestination = new Uri(UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlAcs));
            var singleLogoutDestination = new Uri(UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlSingleLogout));

            var entityDescriptor = new EntityDescriptor(samlConfig);
            if (party != null)
            {
                entityDescriptor.ValidUntil = new TimeSpan(0, 0, settings.SamlMetadataLifetime).Days;
            }
            entityDescriptor.SPSsoDescriptor = new SPSsoDescriptor
            {
                //AuthnRequestsSigned = true,
                //WantAssertionsSigned = true,
                SigningCertificates = new X509Certificate2[]
                {
                    samlConfig.SigningCertificate
                },
                EncryptionCertificates = new X509Certificate2[]
                {
                    samlConfig.DecryptionCertificate
                },
                AssertionConsumerServices = new AssertionConsumerService[]
                {
                    new AssertionConsumerService { Binding = ToSamleBindingUri(party?.AuthnBinding?.ResponseBinding), Location = acsDestination, },
                },
            };
            entityDescriptor.SPSsoDescriptor.SingleLogoutServices = new SingleLogoutService[]
            {
                new SingleLogoutService { Binding = ToSamleBindingUri(party?.LogoutBinding?.ResponseBinding), Location = singleLogoutDestination },
            };

            return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
        }

        public async Task<IActionResult> IdPMetadataAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, IdP Metadata request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = RouteBinding.DownParty != null ? await tenantRepository.GetAsync<SamlDownParty>(partyId) : null;

            var samlConfig = saml2ConfigurationLogic.GetSamlDownConfig(party, true);

            var authnDestination = new Uri(UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlAuthn));
            var logoutDestination = new Uri(UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlLogout));

            var entityDescriptor = new EntityDescriptor(samlConfig);
            if (party != null)
            {
                entityDescriptor.ValidUntil = new TimeSpan(0, 0, settings.SamlMetadataLifetime).Days;
            }
            entityDescriptor.IdPSsoDescriptor = new IdPSsoDescriptor
            {
                SigningCertificates = new X509Certificate2[]
                {
                    samlConfig.SigningCertificate
                },
                //EncryptionCertificates = new X509Certificate2[]
                //{
                //    config.DecryptionCertificate
                //},
                SingleSignOnServices = new SingleSignOnService[]
                {
                    new SingleSignOnService { Binding = ToSamleBindingUri(party?.AuthnBinding?.RequestBinding), Location = authnDestination, },
                },
            };
            entityDescriptor.IdPSsoDescriptor.SingleLogoutServices = new SingleLogoutService[]
            {
                new SingleLogoutService { Binding = ToSamleBindingUri(party?.LogoutBinding?.RequestBinding), Location = logoutDestination },
            };

            return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
        }

        private Uri ToSamleBindingUri(SamlBindingTypes? binding)
        {
            switch (binding)
            {
                case SamlBindingTypes.Redirect:
                    return ProtocolBindings.HttpRedirect;
                case SamlBindingTypes.Post:
                case null:
                    return ProtocolBindings.HttpPost;
                default:
                    throw new NotSupportedException($"SAML binding '{binding}' not supported.");
            }
        }
    }
}
