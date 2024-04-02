using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SamlMetadataReadLogic
    {
        private readonly IHttpClientFactory httpClientFactory;

        public SamlMetadataReadLogic(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task PopulateModelAsync(SamlUpParty party)
        {
            var metadata = await ReadMetadataAsync(party.MetadataUrl);
            await PopulateModelAsync(party, metadata);
        }

        private async Task<string> ReadMetadataAsync(string metadataUrl)
        {
            var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, metadataUrl));
            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var metadata = await response.Content.ReadAsStringAsync();
                    return metadata;

                default:
                    throw new Exception($"Status Code OK expected. Unable to read SAML 2.0 metadata '{metadataUrl}'. StatusCode={response.StatusCode}..");
            }
        }

        public async Task PopulateModelAsync(SamlUpParty party, string metadataXml)
        {
            if(metadataXml?.Length > Constants.Models.SamlParty.MetadataXmlSize)
            {
                throw new Exception($"Metadata XML must be a string with a maximum length of '{Constants.Models.SamlParty.MetadataXmlSize}'.");
            }

            var entityDescriptor = new EntityDescriptor();
            entityDescriptor.ReadIdPSsoDescriptor(metadataXml);
            await PopulateModelInternalAsync(party, entityDescriptor);
        }

        private async Task PopulateModelInternalAsync(SamlUpParty party, EntityDescriptor entityDescriptor)
        {
            if (entityDescriptor.IdPSsoDescriptor != null)
            {
                party.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                party.Issuer = entityDescriptor.EntityId;
                var singleSignOnServices = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.FirstOrDefault();
                if (singleSignOnServices == null)
                {
                    throw new Exception("IdPSsoDescriptor SingleSignOnServices is empty.");
                }

                party.AuthnUrl = singleSignOnServices.Location?.OriginalString;
                party.AuthnBinding.RequestBinding = GetSamlBindingTypes(singleSignOnServices.Binding?.OriginalString);

                var singleLogoutDestination = GetSingleLogoutServices(entityDescriptor.IdPSsoDescriptor.SingleLogoutServices);
                if (singleLogoutDestination != null)
                {
                    party.LogoutUrl = singleLogoutDestination.Location?.OriginalString;
                    var singleLogoutResponseLocation = singleLogoutDestination.ResponseLocation?.OriginalString;
                    if (!string.IsNullOrEmpty(singleLogoutResponseLocation))
                    {
                        party.SingleLogoutResponseUrl = singleLogoutResponseLocation;
                    }
                    if (party.LogoutBinding == null)
                    {
                        party.LogoutBinding = new SamlBinding { ResponseBinding = SamlBindingTypes.Post };
                    }
                    party.LogoutBinding.RequestBinding = GetSamlBindingTypes(singleLogoutDestination.Binding?.OriginalString);
                }

                if (entityDescriptor.IdPSsoDescriptor.SigningCertificates?.Count() > 0)
                {
                    party.Keys = await Task.FromResult(entityDescriptor.IdPSsoDescriptor.SigningCertificates.Select(c => c.ToFTJsonWebKey()).ToList());
                }
                else
                {
                    party.Keys = null;
                }

                if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
                {
                    party.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
                }
            }
            else
            {
                throw new Exception("IdPSsoDescriptor not loaded from metadata.");
            }
        }

        private SingleLogoutService GetSingleLogoutServices(IEnumerable<SingleLogoutService> singleLogoutServices)
        {
            var singleLogoutService = singleLogoutServices.Where(s => s.Binding.OriginalString == ProtocolBindings.HttpPost.OriginalString).FirstOrDefault();
            if (singleLogoutService != null)
            {
                return singleLogoutService;
            }
            else
            {
                return singleLogoutServices.FirstOrDefault();
            }
        }

        private SamlBindingTypes GetSamlBindingTypes(string binding)
        {
            if (binding == ProtocolBindings.HttpPost.OriginalString)
            {
                return SamlBindingTypes.Post;
            }
            else if (binding == ProtocolBindings.HttpRedirect.OriginalString)
            {
                return SamlBindingTypes.Redirect;
            }
            else
            {
                throw new Exception($"Binding '{binding}' not supported.");
            }
        }
    }
}
