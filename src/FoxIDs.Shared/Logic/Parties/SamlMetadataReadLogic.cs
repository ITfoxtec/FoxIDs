using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SamlMetadataReadLogic
    {
        public async Task PopulateModelAsync(SamlUpParty party)
        {
            var entityDescriptor = new EntityDescriptor();
            entityDescriptor.ReadIdPSsoDescriptorFromUrl(new Uri(party.MetadataUrl));
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
