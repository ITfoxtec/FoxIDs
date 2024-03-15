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
using MTokens = Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using FoxIDs.Client.Shared.Components;
using BlazorInputFile;
using Microsoft.AspNetCore.WebUtilities;
using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EOidcUpParty : UpPartyBase
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        protected List<string> responseTypeItems = new List<string> (Constants.Oidc.DefaultResponseTypes);
        private Modal importClientKeyModal;
        private PageEditForm<OAuthUpImportClientKeyViewModel> importClientKeyForm;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (!UpParty.CreateMode)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                var generalOidcUpParty = UpParty as GeneralOidcUpPartyViewModel;
                var oidcUpParty = await UpPartyService.GetOidcUpPartyAsync(UpParty.Name);
                var clientKeyResponse = await UpPartyService.GetOidcClientKeyUpPartyAsync(UpParty.Name);
                await generalOidcUpParty.Form.InitAsync(ToViewModel(generalOidcUpParty, oidcUpParty, clientKeyResponse));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                UpParty.Error = ex.Message;
            }
        }

        private OidcUpPartyViewModel ToViewModel(GeneralOidcUpPartyViewModel generalOidcUpParty, OidcUpParty oidcUpParty, OAuthClientKeyResponse clientKeyResponse)
        {

            return oidcUpParty.Map<OidcUpPartyViewModel>(afterMap =>
            {
                if (afterMap.DisplayName.IsNullOrWhiteSpace())
                {
                    afterMap.DisplayName = afterMap.Name;
                }

                if (oidcUpParty.UpdateState == PartyUpdateStates.Manual)
                {
                    afterMap.IsManual = true;
                }

                if (oidcUpParty.UpdateState == PartyUpdateStates.AutomaticStopped)
                {
                    afterMap.AutomaticStopped = true;
                }
                else
                {
                    afterMap.AutomaticStopped = false;
                }

                if (afterMap.Client != null)
                {
                    afterMap.Client.Party = afterMap;

                    if (afterMap.Client.ClientSecret != null)
                    {
                        afterMap.Client.ClientSecret = afterMap.Client.ClientSecretLoaded = afterMap.Client.ClientSecret.Length == 3 ? $"{afterMap.Client.ClientSecret}..." : afterMap.Client.ClientSecret;
                    }

                    if (clientKeyResponse?.PrimaryKey?.PublicKey != null)
                    {
                        afterMap.Client.PublicClientKeyInfo = new KeyInfoViewModel
                        {
                            Subject = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.Subject,
                            ValidFrom = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.ValidFrom,
                            ValidTo = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.ValidTo,
                            IsValid = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.IsValid(),
                            Thumbprint = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.Thumbprint,
                            KeyId = clientKeyResponse.PrimaryKey.PublicKey.Kid,
                            Key = clientKeyResponse.PrimaryKey.PublicKey,
                            Name = clientKeyResponse.Name
                        };
                    }
                }

                generalOidcUpParty.KeyInfoList.Clear();
                foreach (var key in afterMap.Keys)
                {
                    if (key.Kty == MTokens.JsonWebAlgorithmsKeyTypes.RSA && key.X5c?.Count >= 1)
                    {
                        generalOidcUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = key.CertificateInfo.Subject,
                            ValidFrom = key.CertificateInfo.ValidFrom,
                            ValidTo = key.CertificateInfo.ValidTo,
                            IsValid = key.CertificateInfo.IsValid(),
                            Thumbprint = key.CertificateInfo.Thumbprint,
                            KeyId = key.Kid,
                            Key = key
                        });
                    }
                    else
                    {
                        generalOidcUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            KeyId = key.Kid,
                            Key = key
                        });
                    }
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void OidcUpPartyViewModelAfterInit(GeneralOidcUpPartyViewModel oidcUpParty, OidcUpPartyViewModel model)
        {
            if (oidcUpParty.CreateMode)
            {
                model.Client = new OidcUpClientViewModel();
                model.Client.Claims = new List<string> { "*" };
            }

            if (model.Client != null && model.Client.Party == null)
            {
                model.Client.Party = model;
            }
        }

        private async Task OnEditOidcUpPartyValidSubmitAsync(GeneralOidcUpPartyViewModel generalOidcUpParty, EditContext editContext)
        {
            try
            {
                if(generalOidcUpParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalOidcUpParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var oidcUpParty = generalOidcUpParty.Form.Model.Map<OidcUpParty>(afterMap: afterMap =>
                {
                    afterMap.UpdateState = PartyUpdateStates.Automatic;

                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                    
                    if (!(afterMap.LinkExternalUser?.AutoCreateUser == true || afterMap.LinkExternalUser?.RequireUser == true))
                    {
                        afterMap.LinkExternalUser = null;
                    }
                });

                if (generalOidcUpParty.CreateMode)
                {
                    var oidcUpPartyResult = await UpPartyService.CreateOidcUpPartyAsync(oidcUpParty);
                    generalOidcUpParty.Form.UpdateModel(ToViewModel(generalOidcUpParty, oidcUpPartyResult, null));
                    generalOidcUpParty.CreateMode = false;
                    toastService.ShowSuccess("OpenID Connect application created.");
                    generalOidcUpParty.Name = oidcUpPartyResult.Name;
                    generalOidcUpParty.DisplayName = oidcUpPartyResult.DisplayName;
                }
                else
                {
                    var deleteClientSecret = false;
                    if (oidcUpParty.Client != null && oidcUpParty.Client.ClientSecret != generalOidcUpParty.Form.Model.Client.ClientSecretLoaded)
                    {
                        if (string.IsNullOrWhiteSpace(oidcUpParty.Client.ClientSecret))
                        {
                            deleteClientSecret = true;
                        }
                        else
                        {
                            await UpPartyService.UpdateOidcClientSecretUpPartyAsync(new OAuthClientSecretSingleRequest { PartyName = UpParty.Name, Secret = oidcUpParty.Client.ClientSecret });
                        }
                        oidcUpParty.Client.ClientSecret = null;
                    }

                    var oidcUpPartyResult = await UpPartyService.UpdateOidcUpPartyAsync(oidcUpParty);
                    if (deleteClientSecret)
                    {
                        await UpPartyService.DeleteOidcClientSecretUpPartyAsync(UpParty.Name);
                        oidcUpPartyResult.Client.ClientSecret = null;
                    }
                    var clientKeyResponse = await UpPartyService.GetOidcClientKeyUpPartyAsync(UpParty.Name);
                    generalOidcUpParty.Form.UpdateModel(ToViewModel(generalOidcUpParty, oidcUpPartyResult, clientKeyResponse));
                    toastService.ShowSuccess("OpenID Connect application updated.");
                    generalOidcUpParty.DisplayName = oidcUpPartyResult.DisplayName;
                }                
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalOidcUpParty.Form.SetFieldError(nameof(generalOidcUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteOidcUpPartyAsync(GeneralOidcUpPartyViewModel generalOidcUpParty)
        {
            try
            {
                await UpPartyService.DeleteOidcUpPartyAsync(generalOidcUpParty.Name);
                UpParties.Remove(generalOidcUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalOidcUpParty.Form.SetError(ex.Message);
            }
        }

        private void ShowImportClientKeyPopup()
        {
            importClientKeyForm.Model.Password = null;
            importClientKeyForm.ClearFieldError(nameof(importClientKeyForm.Model.ClientKeyFileStatus));
            importClientKeyForm.Model.ClientKeyFileStatus = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;
            importClientKeyForm.Model.PublicClientKeyInfo = null;
            importClientKeyModal.Show();
        }

        private async Task OnImportClientKeyFileAsync(GeneralOidcUpPartyViewModel oidcUpParty, IFileListEntry[] files)
        {
            try
            {
                importClientKeyForm.ClearFieldError(nameof(importClientKeyForm.Model.ClientKeyFileStatus));
                foreach (var file in files)
                {
                    if (file.Size > GeneralTrackCertificateViewModel.CertificateMaxFileSize)
                    {
                        importClientKeyForm.SetFieldError(nameof(importClientKeyForm.Model.ClientKeyFileStatus), $"That's too big. Max size: {GeneralTrackCertificateViewModel.CertificateMaxFileSize} bytes.");
                        return;
                    }

                    importClientKeyForm.Model.ClientKeyFileStatus = "Loading...";

                    byte[] certificateBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.Data.CopyToAsync(memoryStream);
                        certificateBytes = memoryStream.ToArray();
                    }

                    var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(certificateBytes);
                    var clientKeyResponse = await UpPartyService.CreateOidcClientKeyUpPartyAsync(new OAuthClientKeyRequest { Type = ClientKeyTypes.KeyVaultImport, PartyName = UpParty.Name, Certificate = base64UrlEncodeCertificate, Password = importClientKeyForm.Model.Password });

                    oidcUpParty.Form.Model.Client.PublicClientKeyInfo = importClientKeyForm.Model.PublicClientKeyInfo = new KeyInfoViewModel
                    {
                        Subject = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.Subject,
                        ValidFrom = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.ValidFrom,
                        ValidTo = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.ValidTo,
                        IsValid = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.IsValid(),
                        Thumbprint = clientKeyResponse.PrimaryKey.PublicKey.CertificateInfo.Thumbprint,
                        KeyId = clientKeyResponse.PrimaryKey.PublicKey.Kid,
                        Key = clientKeyResponse.PrimaryKey.PublicKey,
                        Name = clientKeyResponse.Name
                    };

                    importClientKeyForm.Model.ClientKeyFileStatus = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;
                    importClientKeyModal.Hide();
                    toastService.ShowSuccess("Authentication method client key imported.");
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                importClientKeyForm.Model.ClientKeyFileStatus = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;
                importClientKeyForm.SetFieldError(nameof(importClientKeyForm.Model.ClientKeyFileStatus), ex.Message);
            }
            catch (FoxIDsApiException aex)
            {
                importClientKeyForm.Model.ClientKeyFileStatus = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;
                importClientKeyForm.SetFieldError(nameof(importClientKeyForm.Model.ClientKeyFileStatus), aex.Message);
            }
        }

        private async Task RemoveClientKeyAsync(GeneralOidcUpPartyViewModel oidcUpParty, string keyName)
        {
            await UpPartyService.DeleteOidcClientKeyUpPartyAsync($"{oidcUpParty.Name}.{keyName}");

            oidcUpParty.Form.Model.Client.PublicClientKeyInfo = null;
            toastService.ShowSuccess("Authentication method client key removed.");
        }

        private async Task DownloadPublicCertificateFileAsync(KeyInfoViewModel publicClientKeyInfo)
        {
            await JSRuntime.InvokeAsync<object>("saveCertFile", $"{publicClientKeyInfo.Subject}.cer", publicClientKeyInfo.Key.X5c.First());
        }
    }
}
