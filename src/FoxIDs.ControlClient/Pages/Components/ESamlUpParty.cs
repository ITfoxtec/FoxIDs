using FoxIDs.Client.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using BlazorInputFile;
using ITfoxtec.Identity.Models;
using System.Security.Claims;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Components;
using Tewr.Blazor.FileReader;
using System.Text;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.WebUtilities;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ESamlUpParty : UpPartyBase
    {
        private ElementReference readMetadataFileElement;

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Inject]
        public IFileReaderService fileReaderService { get; set; }

        private void SamlUpPartyViewModelAfterInit(GeneralSamlUpPartyViewModel samlUpParty, SamlUpPartyViewModel model)
        {
            if (samlUpParty.CreateMode)
            {
                model.Claims = new List<string> { ClaimTypes.Email, ClaimTypes.Name, ClaimTypes.GivenName, ClaimTypes.Surname };
            }
        }

        private async Task OnReadMetadataFileAsync(GeneralSamlUpPartyViewModel generalSamlUpParty)
        {
            generalSamlUpParty.Form.ClearError();
            try
            {
                var files = await fileReaderService.CreateReference(readMetadataFileElement).EnumerateFilesAsync();
                var file = files.FirstOrDefault();
                if (file == null)
                {
                    return;
                }

                string metadata;
                await using (var stream = await file.OpenReadAsync())
                {
                    byte[] resultBytes = new byte[stream.Length];
                    await stream.ReadAsync(resultBytes, 0, (int)stream.Length);

                    metadata = Encoding.ASCII.GetString(resultBytes);
                }

                var entityDescriptor = new EntityDescriptor();
                entityDescriptor.ReadIdPSsoDescriptor(metadata);
                if (entityDescriptor.IdPSsoDescriptor != null)
                {
                    generalSamlUpParty.Form.Model.Issuer = entityDescriptor.EntityId;
                    var singleSignOnServices = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.FirstOrDefault();
                    if (singleSignOnServices == null)
                    {
                        throw new Exception("IdPSsoDescriptor SingleSignOnServices is empty.");
                    }

                    generalSamlUpParty.Form.Model.AuthnUrl = singleSignOnServices.Location?.OriginalString;
                    generalSamlUpParty.Form.Model.AuthnRequestBinding = GetSamlBindingTypes(singleSignOnServices.Binding?.OriginalString);

                    var singleLogoutDestination = GetSingleLogoutServices(entityDescriptor.IdPSsoDescriptor.SingleLogoutServices);
                    if (singleLogoutDestination != null)
                    {
                        generalSamlUpParty.Form.Model.LogoutUrl = singleLogoutDestination.Location?.OriginalString;
                        var singleLogoutResponseLocation = singleLogoutDestination.ResponseLocation?.OriginalString;
                        if (!string.IsNullOrEmpty(singleLogoutResponseLocation))
                        {
                            generalSamlUpParty.Form.Model.SingleLogoutResponseUrl = singleLogoutResponseLocation;
                        }
                        generalSamlUpParty.Form.Model.LogoutRequestBinding = GetSamlBindingTypes(singleLogoutDestination.Binding?.OriginalString);
                    }

                    generalSamlUpParty.KeyInfoList = new List<KeyInfoViewModel>();
                    generalSamlUpParty.Form.Model.Keys = new List<JsonWebKey>();
                    if (entityDescriptor.IdPSsoDescriptor.SigningCertificates?.Count() > 0)
                    {
                        foreach(var certificate in entityDescriptor.IdPSsoDescriptor.SigningCertificates)
                        {
                            var jwk = await certificate.ToFTJsonWebKeyAsync();

                            generalSamlUpParty.KeyInfoList.Add(new KeyInfoViewModel
                            {
                                Subject = certificate.Subject,
                                ValidFrom = certificate.NotBefore,
                                ValidTo = certificate.NotAfter,
                                IsValid = certificate.IsValid(),
                                Thumbprint = certificate.Thumbprint,
                                Key = jwk
                            });
                            generalSamlUpParty.Form.Model.Keys.Add(jwk);
                        }

                        generalSamlUpParty.Form.Model.Keys = await Task.FromResult(entityDescriptor.IdPSsoDescriptor.SigningCertificates.Select(c => c.ToFTJsonWebKey()).ToList());
                    }

                    if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
                    {
                        generalSamlUpParty.Form.Model.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
                    }
                }
                else
                {
                    throw new Exception("IdPSsoDescriptor not loaded from metadata.");
                }
            }
            catch (Exception ex)
            {
                generalSamlUpParty.Form.SetError($"Failing SAML 2.0 metadata. {ex.Message}");
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

        private async Task OnSamlUpPartyCertificateFileSelectedAsync(GeneralSamlUpPartyViewModel generalSamlUpParty, IFileListEntry[] files)
        {
            if (generalSamlUpParty.Form.Model.Keys == null)
            {
                generalSamlUpParty.Form.Model.Keys = new List<JwtWithCertificateInfo>();
            }
            generalSamlUpParty.Form.ClearFieldError(nameof(generalSamlUpParty.Form.Model.Keys));
            foreach (var file in files)
            {
                if (file.Size > GeneralSamlUpPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), $"That's too big. Max size: {GeneralSamlUpPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlUpParty.CertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    await file.Data.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwtWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        if (generalSamlUpParty.Form.Model.Keys.Any(k => k.X5t.Equals(jwtWithCertificateInfo.X5t, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        generalSamlUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = jwtWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwtWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwtWithCertificateInfo.CertificateInfo.ValidTo,
                            IsValid = jwtWithCertificateInfo.CertificateInfo.IsValid(),
                            Thumbprint = jwtWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwtWithCertificateInfo
                        });
                        generalSamlUpParty.Form.Model.Keys.Add(jwtWithCertificateInfo);
                    }
                    catch (Exception ex)
                    {
                        generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), ex.Message);
                    }
                }

                generalSamlUpParty.CertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveSamlUpPartyCertificate(GeneralSamlUpPartyViewModel generalSamlUpParty, KeyInfoViewModel keyInfo)
        {
            generalSamlUpParty.Form.ClearFieldError(nameof(generalSamlUpParty.Form.Model.Keys));
            if (generalSamlUpParty.Form.Model.Keys.Remove(keyInfo.Key))
            {
                generalSamlUpParty.KeyInfoList.Remove(keyInfo);
            }
        }

        private async Task OnEditSamlUpPartyValidSubmitAsync(GeneralSamlUpPartyViewModel generalSamlUpParty, EditContext editContext)
        {
            try
            {
                if (generalSamlUpParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalSamlUpParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is SamlClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var samlUpParty = generalSamlUpParty.Form.Model.Map<SamlUpParty>(afterMap =>
                {
                    afterMap.DisableSingleLogout = !generalSamlUpParty.Form.Model.EnableSingleLogout;

                    if (generalSamlUpParty.Form.Model.AuthnContextComparisonViewModel != SamlAuthnContextComparisonTypesVievModel.Null)
                    {
                        afterMap.AuthnContextComparison = (SamlAuthnContextComparisonTypes)Enum.Parse(typeof(SamlAuthnContextComparisonTypes), generalSamlUpParty.Form.Model.AuthnContextComparisonViewModel.ToString());
                    }                    

                    if (generalSamlUpParty.Form.Model.IsManual)
                    {
                        afterMap.UpdateState = PartyUpdateStates.Manual;

                    }
                    else
                    {
                        afterMap.UpdateState = PartyUpdateStates.Automatic;
                    }

                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                });

                if (generalSamlUpParty.CreateMode)
                {
                    await UpPartyService.CreateSamlUpPartyAsync(samlUpParty);
                }
                else
                {
                    await UpPartyService.UpdateSamlUpPartyAsync(samlUpParty);
                }
                generalSamlUpParty.Name = generalSamlUpParty.Form.Model.Name;
                generalSamlUpParty.Edit = false;
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSamlUpPartyAsync(GeneralSamlUpPartyViewModel generalSamlUpParty)
        {
            try
            {
                await UpPartyService.DeleteSamlUpPartyAsync(generalSamlUpParty.Name);
                UpParties.Remove(generalSamlUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalSamlUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
