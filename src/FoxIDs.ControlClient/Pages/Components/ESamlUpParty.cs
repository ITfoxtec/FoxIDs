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

namespace FoxIDs.Client.Pages.Components
{
    public partial class ESamlUpParty : UpPartyBase
    {
        private ElementReference readMetadataFileElement;

        [Inject]
        public IFileReaderService fileReaderService { get; set; }

        private void SamlUpPartyViewModelAfterInit(GeneralSamlUpPartyViewModel samlUpParty, SamlUpPartyViewModel model)
        {
            if (samlUpParty.CreateMode)
            {
                model.Claims = new List<string> { ClaimTypes.Email, ClaimTypes.Name, ClaimTypes.GivenName, ClaimTypes.Surname };
            }
        }

        private void SetMode(SamlUpPartyViewModel samlUpParty, SamlPartyModes mode)
        {
            samlUpParty.Mode = mode;
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

                    generalSamlUpParty.CertificateInfoList = new List<CertificateInfoViewModel>();
                    generalSamlUpParty.Form.Model.Keys = new List<JsonWebKey>();
                    if (entityDescriptor.IdPSsoDescriptor.SigningCertificates?.Count() > 0)
                    {
                        foreach(var certificate in entityDescriptor.IdPSsoDescriptor.SigningCertificates)
                        {
                            var jwk = await certificate.ToFTJsonWebKeyAsync();

                            generalSamlUpParty.CertificateInfoList.Add(new CertificateInfoViewModel
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
                generalSamlUpParty.Form.Model.Keys = new List<JsonWebKey>();
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
                        var certificate = new X509Certificate2(memoryStream.ToArray());
                        var jwk = await certificate.ToFTJsonWebKeyAsync();

                        if (generalSamlUpParty.Form.Model.Keys.Any(k => k.X5t.Equals(jwk.X5t, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        generalSamlUpParty.CertificateInfoList.Add(new CertificateInfoViewModel
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
                    catch (Exception ex)
                    {
                        generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), ex.Message);
                    }
                }

                generalSamlUpParty.CertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveSamlUpPartyCertificate(GeneralSamlUpPartyViewModel generalSamlUpParty, CertificateInfoViewModel certificateInfo)
        {
            generalSamlUpParty.Form.ClearFieldError(nameof(generalSamlUpParty.Form.Model.Keys));
            if (generalSamlUpParty.Form.Model.Keys.Remove(certificateInfo.Key))
            {
                generalSamlUpParty.CertificateInfoList.Remove(certificateInfo);
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

                    afterMap.AuthnBinding = new SamlBinding { RequestBinding = generalSamlUpParty.Form.Model.AuthnRequestBinding, ResponseBinding = generalSamlUpParty.Form.Model.AuthnResponseBinding };
                    if (!afterMap.LogoutUrl.IsNullOrEmpty())
                    {
                        afterMap.LogoutBinding = new SamlBinding { RequestBinding = generalSamlUpParty.Form.Model.LogoutRequestBinding, ResponseBinding = generalSamlUpParty.Form.Model.LogoutResponseBinding };
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
