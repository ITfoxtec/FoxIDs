using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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

        public async Task<SamlUpParty> PopulateModelAsync(SamlUpParty party)
        {
            var metadata = await ReadMetadataAsync(party.MetadataUrl);
            return await PopulateModelAsync(party, metadata);
        }

        public async Task<SamlDownParty> PopulateModelAsync(SamlDownParty party)
        {
            var metadata = await ReadMetadataAsync(party.MetadataUrl);
            return await PopulateModelAsync(party, metadata);
        }

        private async Task<string> ReadMetadataAsync(string metadataUrl)
        {
            try
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
                        throw new Exception($"SAML 2.0 metadata response error, status code={response.StatusCode}.");
                }
            }
            catch (HttpRequestException hrex)
            {
                if (hrex.InnerException is SocketException soex)
                {
                    if (soex.SocketErrorCode == SocketError.TimedOut)
                    {
                        throw new Exception($"It is not possible to call the SAML 2.0 metadata URL '{metadataUrl}', the call has timed out.", hrex);
                    }
                }

                throw new Exception($"It is not possible to call the SAML 2.0 metadata URL '{metadataUrl}'.", hrex);
            }
            catch (TaskCanceledException tcex)
            {
                if (tcex.InnerException is TimeoutException)
                {
                    throw new Exception($"It is not possible to call the SAML 2.0 metadata URL '{metadataUrl}', the call has timed out.", tcex);
                }

                throw new Exception($"It is not possible to call the SAML 2.0 metadata URL '{metadataUrl}'.", tcex);
            }
            catch (Exception ex)
            {
                throw new Exception($"The call to the SAML 2.0 metadata URL '{metadataUrl}' has failed.", ex);
            }
        }

        public async Task<SamlUpParty> PopulateModelAsync(SamlUpParty party, string metadataXml)
        {
            if(metadataXml?.Length > Constants.Models.SamlParty.MetadataXmlSize)
            {
                throw new Exception($"Metadata XML must be a string with a maximum length of '{Constants.Models.SamlParty.MetadataXmlSize}'.");
            }

            var entityDescriptor = new EntityDescriptor();
            entityDescriptor.ReadIdPSsoDescriptor(metadataXml);
            return await PopulateModelInternalAsync(party, entityDescriptor);
        }

        public async Task<SamlDownParty> PopulateModelAsync(SamlDownParty party, string metadataXml)
        {
            if (metadataXml?.Length > Constants.Models.SamlParty.MetadataXmlSize)
            {
                throw new Exception($"Metadata XML must be a string with a maximum length of '{Constants.Models.SamlParty.MetadataXmlSize}'.");
            }

            var entityDescriptor = new EntityDescriptor();
            entityDescriptor.ReadSPSsoDescriptor(metadataXml);
            return await PopulateModelInternalAsync(party, entityDescriptor);
        }

        private async Task<SamlUpParty> PopulateModelInternalAsync(SamlUpParty party, EntityDescriptor entityDescriptor)
        {
            if (entityDescriptor.IdPSsoDescriptor == null)
            {
                throw new Exception("IDPSSODescriptor element is missing from the metadata file.");
            }

            party.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            party.Issuers = [entityDescriptor.EntityId];
            var singleSignOnServices = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.FirstOrDefault();
            if (singleSignOnServices == null)
            {
                throw new Exception("IDPSSODescriptor SingleSignOnServices is empty.");
            }

            party.AuthnUrl = singleSignOnServices.Location?.OriginalString;
            party.AuthnBinding ??= new SamlBinding();
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
            else
            {
                party.LogoutUrl = null;
                party.SingleLogoutResponseUrl = null;
                party.LogoutBinding = null;
            }

            if (entityDescriptor.IdPSsoDescriptor.SigningCertificates?.Count() > 0)
            {
                party.Keys = await Task.FromResult(entityDescriptor.IdPSsoDescriptor.SigningCertificates
                    .Take(Constants.Models.SamlParty.KeysMax)
                    .Select(c => c.ToFTJsonWebKey())
                    .ToList());
            }
            else
            {
                party.Keys = null;
            }

            if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
            {
                party.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
            }
            else
            {
                party.SignAuthnRequest = false;
            }

            return party;
        }

        private async Task<SamlDownParty> PopulateModelInternalAsync(SamlDownParty party, EntityDescriptor entityDescriptor)
        {
            if (entityDescriptor.SPSsoDescriptor == null)
            {
                throw new Exception("SPSSODescriptor element is missing from the metadata file.");
            }

            party.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            party.Issuer = entityDescriptor.EntityId;

            var assertionConsumerServices = entityDescriptor.SPSsoDescriptor.AssertionConsumerServices?.ToList();
            if (assertionConsumerServices == null || assertionConsumerServices.Count == 0)
            {
                throw new Exception("SPSSODescriptor AssertionConsumerServices is empty.");
            }

            party.AcsUrls = assertionConsumerServices
                .Select(s => s.Location?.OriginalString)
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();
            if (party.AcsUrls.Count == 0)
            {
                throw new Exception("SPSSODescriptor AssertionConsumerServices is empty.");
            }

            party.DisableAbsoluteUrls = false;

            var defaultAcs = assertionConsumerServices.FirstOrDefault(s => s.IsDefault == true) ?? assertionConsumerServices.First();
            party.AuthnBinding ??= new SamlBinding();
            party.AuthnBinding.ResponseBinding = GetSamlBindingTypes(defaultAcs.Binding?.OriginalString);

            var singleLogoutDestination = GetSingleLogoutServices(entityDescriptor.SPSsoDescriptor.SingleLogoutServices);
            if (singleLogoutDestination != null)
            {
                party.SingleLogoutUrl = singleLogoutDestination.Location?.OriginalString;
                if (party.LogoutBinding == null)
                {
                    party.LogoutBinding = new SamlBinding { RequestBinding = SamlBindingTypes.Post };
                }
                party.LogoutBinding.ResponseBinding = GetSamlBindingTypes(singleLogoutDestination.Binding?.OriginalString);
            }
            else
            {
                party.SingleLogoutUrl = null;
                party.LogoutBinding = null;
            }

            if (entityDescriptor.SPSsoDescriptor.SigningCertificates?.Count() > 0)
            {
                party.Keys = await Task.FromResult(entityDescriptor.SPSsoDescriptor.SigningCertificates
                    .Take(Constants.Models.SamlParty.KeysMax)
                    .Select(c => c.ToFTJsonWebKey())
                    .ToList());
            }
            else
            {
                party.Keys = null;
            }

            if (entityDescriptor.SPSsoDescriptor.EncryptionCertificates?.Count() > 0)
            {
                party.EncryptionKey = entityDescriptor.SPSsoDescriptor.EncryptionCertificates.First().ToFTJsonWebKey();
                party.EncryptAuthnResponse = true;
            }
            else
            {
                party.EncryptionKey = null;
                party.EncryptAuthnResponse = false;
            }

            return party;
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
