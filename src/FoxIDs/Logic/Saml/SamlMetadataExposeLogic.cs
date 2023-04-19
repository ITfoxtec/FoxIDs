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
using ITfoxtec.Identity.Util;
using FoxIDs.Models.Config;
using System.Collections.Generic;
using ITfoxtec.Identity;
using System.Linq;

namespace FoxIDs.Logic
{
    public class SamlMetadataExposeLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;

        public SamlMetadataExposeLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ITenantRepository tenantRepository, Saml2ConfigurationLogic saml2ConfigurationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
        }

        public async Task<IActionResult> SpMetadataAsync(string partyId)
        {
            logger.ScopeTrace(() => "Up, SP Metadata request.");
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);
            var party = RouteBinding.UpParty != null ? await tenantRepository.GetAsync<SamlUpParty>(partyId) : null;
            var signMetadata = party != null ? party.SignMetadata : false;

            var samlConfig = await saml2ConfigurationLogic.GetSamlUpConfigAsync(party, includeSigningAndDecryptionCertificate: signMetadata, includeSignatureValidationCertificates: false);

            var acsDestination = new Uri(UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlAcs));
            var singleLogoutDestination = new Uri(UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlSingleLogout));

            var entityDescriptor = new EntityDescriptor(samlConfig, signMetadata);

            var trackCertificates = GetTrackCertificates();
            if (party != null)
            {
                entityDescriptor.ValidUntil = GetMaxCertificateLifetimeInDays(trackCertificates);
            }
            entityDescriptor.SPSsoDescriptor = new SPSsoDescriptor
            {
                //AuthnRequestsSigned = true,
                //WantAssertionsSigned = true,
                SigningCertificates = trackCertificates,
                AssertionConsumerServices = new AssertionConsumerService[]
                {
                    new AssertionConsumerService { Binding = ToSamleBindingUri(party?.AuthnBinding?.ResponseBinding), Location = acsDestination },
                },
            };
            entityDescriptor.SPSsoDescriptor.SingleLogoutServices = new SingleLogoutService[]
            {
                new SingleLogoutService { Binding = ToSamleBindingUri(party?.LogoutBinding?.ResponseBinding), Location = singleLogoutDestination, ResponseLocation = party?.MetadataAddLogoutResponseLocation == true ? singleLogoutDestination : null },
            };

            if (party?.MetadataIncludeEncryptionCertificates == true)
            {
                entityDescriptor.SPSsoDescriptor.EncryptionCertificates = trackCertificates;
                entityDescriptor.SPSsoDescriptor.SetDefaultEncryptionMethods();
            }

            if (party?.MetadataNameIdFormats?.Count > 0)
            {
                entityDescriptor.SPSsoDescriptor.NameIDFormats = party.MetadataNameIdFormats.Select(nf => new Uri(nf));
            }

            if (party?.MetadataAttributeConsumingServices?.Count() > 0)
            {
                var attributeConsumingServices = new List<AttributeConsumingService>();
                foreach(var aItem in party.MetadataAttributeConsumingServices)
                {
                    var attributeConsumingService = new AttributeConsumingService { ServiceName = new ServiceName(aItem.ServiceName.Name, aItem.ServiceName.Lang) };
                    attributeConsumingService.RequestedAttributes = aItem.RequestedAttributes.Select(ra => string.IsNullOrEmpty(ra.NameFormat) ? new RequestedAttribute(ra.Name, ra.IsRequired) : new RequestedAttribute(ra.Name, ra.IsRequired, ra.NameFormat));
                    attributeConsumingServices.Add(attributeConsumingService);
                }
                entityDescriptor.SPSsoDescriptor.AttributeConsumingServices = attributeConsumingServices;
            }

            if (party?.MetadataContactPersons?.Count() > 0)
            {
                entityDescriptor.ContactPersons = GetContactPersons(party.MetadataContactPersons);
            }

            return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
        }

        public async Task<IActionResult> IdPMetadataAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, IdP Metadata request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = RouteBinding.DownParty != null ? await tenantRepository.GetAsync<SamlDownParty>(partyId) : null;
            var signMetadata = party != null ? party.SignMetadata : false;

            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party, includeSigningCertificate: signMetadata, includeSignatureValidationCertificates: false);

            var authnDestination = new Uri(UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlAuthn));
            var logoutDestination = new Uri(UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), RouteBinding.PartyNameAndBinding, Constants.Routes.SamlController, Constants.Endpoints.SamlLogout));

            var entityDescriptor = new EntityDescriptor(samlConfig, signMetadata);

            var trackCertificates = GetTrackCertificates();
            if (party != null)
            {
                entityDescriptor.ValidUntil = GetMaxCertificateLifetimeInDays(trackCertificates);
            }
            entityDescriptor.IdPSsoDescriptor = new IdPSsoDescriptor
            {
                SigningCertificates = trackCertificates,
                SingleSignOnServices = new SingleSignOnService[]
                {
                    new SingleSignOnService { Binding = ToSamleBindingUri(party?.AuthnBinding?.RequestBinding), Location = authnDestination },
                },
            };
            entityDescriptor.IdPSsoDescriptor.SingleLogoutServices = new SingleLogoutService[]
            {
                new SingleLogoutService { Binding = ToSamleBindingUri(party?.LogoutBinding?.RequestBinding), Location = logoutDestination, ResponseLocation = party?.MetadataAddLogoutResponseLocation == true ? logoutDestination : null },
            };

            if (party?.MetadataIncludeEncryptionCertificates == true)
            {
                entityDescriptor.IdPSsoDescriptor.EncryptionCertificates = trackCertificates;
                entityDescriptor.IdPSsoDescriptor.SetDefaultEncryptionMethods();
            }

            if (party?.MetadataNameIdFormats?.Count > 0)
            {
                entityDescriptor.IdPSsoDescriptor.NameIDFormats = party.MetadataNameIdFormats.Select(nf => new Uri(nf));
            }

            if (party?.MetadataContactPersons?.Count() > 0)
            {
                entityDescriptor.ContactPersons = GetContactPersons(party.MetadataContactPersons);
            }

            return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
        }

        private int? GetMaxCertificateLifetimeInDays(List<X509Certificate2> trackCertificates)
        {
            var nowLocal = DateTimeOffset.UtcNow.LocalDateTime;
            var days = 0;
            foreach (var cert in trackCertificates) 
            {
                var tempDays = Convert.ToInt32((cert.NotAfter - nowLocal).TotalDays);
                if (tempDays > days)
                {
                    days = tempDays;
                }
            }
            if(days > 0)
            {
                days = days - 1;
            }
            return days > 0 ? days : null;
        }

        private List<X509Certificate2> GetTrackCertificates()
        {
            var trackCertificates = new List<X509Certificate2>
            {
                RouteBinding.Key.PrimaryKey.Key.ToX509Certificate()
            };
            if (RouteBinding.Key.SecondaryKey != null)
            {
                trackCertificates.Add(RouteBinding.Key.SecondaryKey.Key.ToX509Certificate());
            }

            return trackCertificates;
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

        private IEnumerable<ContactPerson> GetContactPersons(List<SamlMetadataContactPerson> metadataContactPersons)
        {
            return metadataContactPersons.Select(cp => new ContactPerson((ContactTypes)Enum.Parse(typeof(ContactTypes), cp.ContactType.ToString()))
            {
                Company = cp.Company,
                GivenName = cp.GivenName,
                SurName = cp.Surname,
                EmailAddress = cp.EmailAddress,
                TelephoneNumber = cp.TelephoneNumber
            });
        }
    }
}
