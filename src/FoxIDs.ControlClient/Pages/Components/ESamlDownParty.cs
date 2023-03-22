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
using BlazorInputFile;
using System.Security.Claims;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components;
using System.Net.Http;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ESamlDownParty : DownPartyBase
    {
        [Inject]
        public HelpersService HelpersService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (!DownParty.CreateMode)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                var generalSamlDownParty = DownParty as GeneralSamlDownPartyViewModel;
                var samlDownParty = await DownPartyService.GetSamlDownPartyAsync(DownParty.Name);
                await generalSamlDownParty.Form.InitAsync(ToViewModel(generalSamlDownParty, samlDownParty));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                DownParty.Error = ex.Message;
            }
        }

        private SamlDownPartyViewModel ToViewModel(GeneralSamlDownPartyViewModel generalSamlDownParty, SamlDownParty samlDownParty)
        {
            return samlDownParty.Map<SamlDownPartyViewModel>(afterMap =>
            {
                generalSamlDownParty.KeyInfoList.Clear();
                if (afterMap.Keys?.Count() > 0)
                {
                    foreach (var key in afterMap.Keys)
                    {
                        generalSamlDownParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = key.CertificateInfo.Subject,
                            ValidFrom = key.CertificateInfo.ValidFrom,
                            ValidTo = key.CertificateInfo.ValidTo,
                            Thumbprint = key.CertificateInfo.Thumbprint,
                            Key = key
                        });
                    }
                }

                if (afterMap.EncryptionKey != null)
                {
                    generalSamlDownParty.EncryptionKeyInfo = new KeyInfoViewModel
                    {
                        Subject = afterMap.EncryptionKey.CertificateInfo.Subject,
                        ValidFrom = afterMap.EncryptionKey.CertificateInfo.ValidFrom,
                        ValidTo = afterMap.EncryptionKey.CertificateInfo.ValidTo,
                        Thumbprint = afterMap.EncryptionKey.CertificateInfo.Thumbprint,
                        Key = afterMap.EncryptionKey
                    };
                }
                else
                {
                    generalSamlDownParty.EncryptionKeyInfo = null;
                }                

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void SamlDownPartyViewModelAfterInit(GeneralSamlDownPartyViewModel samlDownParty, SamlDownPartyViewModel model)
        {
            if (samlDownParty.CreateMode)
            {
                model.Claims = new List<string> { ClaimTypes.Email, ClaimTypes.Name, ClaimTypes.GivenName, ClaimTypes.Surname };
            }
        }

        private async Task OnSamlDownPartyEncryptionCertificateFileSelectedAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, IFileListEntry[] files)
        {
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.EncryptionKey));
            foreach (var file in files)
            {
                if (file.Size > GeneralSamlDownPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.EncryptionKey), $"That's too big. Max size: {GeneralSamlDownPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlDownParty.EncryptionCertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    await file.Data.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwtWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        generalSamlDownParty.EncryptionKeyInfo = new KeyInfoViewModel
                        {
                            Subject = jwtWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwtWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwtWithCertificateInfo.CertificateInfo.ValidTo,
                            Thumbprint = jwtWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwtWithCertificateInfo
                        };
                        generalSamlDownParty.Form.Model.EncryptionKey = jwtWithCertificateInfo;
                    }
                    catch (Exception ex)
                    {
                        generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), ex.Message);
                    }
                }

                generalSamlDownParty.EncryptionCertificateFileStatus = GeneralSamlDownPartyViewModel.DefaultCertificateFileStatus;
                break;
            }
        }

        private void RemoveSamlDownPartyEncryptionCertificate(GeneralSamlDownPartyViewModel generalSamlDownParty, KeyInfoViewModel keyInfo)
        {
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.EncryptionKey));
            generalSamlDownParty.Form.Model.EncryptionKey = null;
        }


        private async Task OnSamlDownPartyCertificateFileSelectedAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, IFileListEntry[] files)
        {
            if (generalSamlDownParty.Form.Model.Keys == null)
            {
                generalSamlDownParty.Form.Model.Keys = new List<JwtWithCertificateInfo>();
            }
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.Keys));
            foreach (var file in files)
            {
                if (file.Size > GeneralSamlDownPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), $"That's too big. Max size: {GeneralSamlDownPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlDownParty.CertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    await file.Data.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwtWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        if (generalSamlDownParty.Form.Model.Keys.Any(k => k.X5t.Equals(jwtWithCertificateInfo.X5t, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        generalSamlDownParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = jwtWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwtWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwtWithCertificateInfo.CertificateInfo.ValidTo,
                            Thumbprint = jwtWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwtWithCertificateInfo
                        });
                        generalSamlDownParty.Form.Model.Keys.Add(jwtWithCertificateInfo);
                    }
                    catch (Exception ex)
                    {
                        generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), ex.Message);
                    }
                }

                generalSamlDownParty.CertificateFileStatus = GeneralSamlDownPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveSamlDownPartyCertificate(GeneralSamlDownPartyViewModel generalSamlDownParty, KeyInfoViewModel keyInfo)
        {
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.Keys));
            if (generalSamlDownParty.Form.Model.Keys.Remove(keyInfo.Key))
            {
                generalSamlDownParty.KeyInfoList.Remove(keyInfo);
            }
        }

        private async Task OnEditSamlDownPartyValidSubmitAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, EditContext editContext)
        {
            try
            {
                if (generalSamlDownParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalSamlDownParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is SamlClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                if(!generalSamlDownParty.Form.Model.EncryptAuthnResponse)
                {
                    generalSamlDownParty.Form.Model.EncryptionKey = null;
                }

                var samlDownParty = generalSamlDownParty.Form.Model.Map<SamlDownParty>(afterMap =>
                {
                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                });

                if (generalSamlDownParty.CreateMode)
                {
                    var samlDownPartyResult = await DownPartyService.CreateSamlDownPartyAsync(samlDownParty);
                    generalSamlDownParty.Form.UpdateModel(ToViewModel(generalSamlDownParty, samlDownPartyResult));
                    generalSamlDownParty.CreateMode = false;
                    toastService.ShowSuccess("SAML 2.0 down-party created.");
                }
                else
                {
                    var samlDownPartyResult = await DownPartyService.UpdateSamlDownPartyAsync(samlDownParty);
                    generalSamlDownParty.Form.UpdateModel(ToViewModel(generalSamlDownParty, samlDownPartyResult));
                    toastService.ShowSuccess("SAML 2.0 down-party updated.");
                }
                generalSamlDownParty.Name = generalSamlDownParty.Form.Model.Name;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSamlDownPartyAsync(GeneralSamlDownPartyViewModel generalSamlDownParty)
        {
            try
            {
                await DownPartyService.DeleteSamlDownPartyAsync(generalSamlDownParty.Name);
                DownParties.Remove(generalSamlDownParty);
                await OnStateHasChanged.InvokeAsync(DownParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalSamlDownParty.Form.SetError(ex.Message);
            }
        }
    }
}
