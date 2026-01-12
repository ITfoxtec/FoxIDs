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
using Microsoft.AspNetCore.WebUtilities;
using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FoxIDs.Util;
using Microsoft.AspNetCore.Components.Web;

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

        private void EnsureOidcUpPartySummaryDefaults(GeneralOidcUpPartyViewModel generalOidcUpParty)
        {
            var model = generalOidcUpParty?.Form?.Model;
            if (model == null)
            {
                return;
            }

            if (model.DisableUserAuthenticationTrust || model.Name.IsNullOrWhiteSpace())
            {
                model.RedirectUrl = null;
                model.PostLogoutRedirectUrl = null;
                model.FrontChannelLogoutUrl = null;
                return;
            }

            var (redirect, postLogoutRedirect, frontChannelLogout) = MetadataLogic.GetUpRedirectAndLogoutUrls(model.Name, model.PartyBindingPattern);
            model.RedirectUrl = redirect;
            model.PostLogoutRedirectUrl = postLogoutRedirect;
            model.FrontChannelLogoutUrl = frontChannelLogout;
        }

        private OidcUpPartyViewModel ToViewModel(GeneralOidcUpPartyViewModel generalOidcUpParty, OidcUpParty oidcUpParty, OAuthClientKeyResponse clientKeyResponse)
        {
            return oidcUpParty.Map<OidcUpPartyViewModel>(afterMap =>
            {
                afterMap.InitName = afterMap.Name;
                if (afterMap.Profiles?.Count() > 0)
                {
                    foreach (var profile in afterMap.Profiles)
                    {
                        profile.InitName = profile.Name;
                    }
                }

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
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapOAuthClaimTransforms();
                }

                afterMap.ExtendedUis.MapExtendedUis();

                if (afterMap.ExitClaimTransforms?.Count > 0)
                {
                    afterMap.ExitClaimTransforms = afterMap.ExitClaimTransforms.MapOAuthClaimTransforms();
                }
                if (afterMap.LinkExternalUser?.ClaimTransforms?.Count > 0)
                {
                    afterMap.LinkExternalUser.ClaimTransforms = afterMap.LinkExternalUser.ClaimTransforms.MapOAuthClaimTransforms();
                }
            });
        }

        private async Task OidcUpPartyViewModelAfterInitAsync(GeneralOidcUpPartyViewModel oidcUpParty, OidcUpPartyViewModel model)
        {
            if (oidcUpParty.CreateMode)
            {
                model.Name = await UpPartyService.GetNewPartyNameAsync();
                model.Client = new OidcUpClientViewModel();
                model.Client.Claims = new List<string> { "*" };
            }

            if (model.Client != null && model.Client.Party == null)
            {
                model.Client.Party = model;
            }
        }

        private void AddProfile(MouseEventArgs e, List<OidcUpPartyProfileViewModel> profiles)
        {
            var profile = new OidcUpPartyProfileViewModel
            {
                Name = RandomName.GenerateDefaultName(profiles.Select(p => p.Name))
            };
            profiles.Add(profile);
        }

        private void RemoveProfile(MouseEventArgs e, List<OidcUpPartyProfileViewModel> profiles, OidcUpPartyProfileViewModel removeProfile)
        {
            profiles.Remove(removeProfile);
        }

        private async Task OnEditOidcUpPartyValidSubmitAsync(GeneralOidcUpPartyViewModel generalOidcUpParty, EditContext editContext)
        {
            try
            {
                generalOidcUpParty.Form.Model.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalOidcUpParty.Form.Model.ExtendedUis.MapExtendedUisBeforeMap();
                generalOidcUpParty.Form.Model.ExitClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalOidcUpParty.Form.Model.LinkExternalUser?.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();

                var oidcUpParty = generalOidcUpParty.Form.Model.Map<OidcUpParty>(afterMap: afterMap =>
                {
                    afterMap.UpdateState = PartyUpdateStates.Automatic;
                    afterMap.Authority = afterMap.Authority.Trim();
                    if(afterMap.Authority.EndsWith(IdentityConstants.OidcDiscovery.Path))
                    {
                        afterMap.Authority = afterMap.Authority.Remove(afterMap.Authority.Length - IdentityConstants.OidcDiscovery.Path.Length);
                    }

                    afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    afterMap.ExtendedUis.MapExtendedUisAfterMap();
                    afterMap.ExitClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    afterMap.LinkExternalUser = afterMap.LinkExternalUser.MapLinkExternalUserAfterMap();
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
                    if (generalOidcUpParty.Form.Model.Name != generalOidcUpParty.Form.Model.InitName)
                    {
                        oidcUpParty.NewName = oidcUpParty.Name;
                        oidcUpParty.Name = generalOidcUpParty.Form.Model.InitName;
                    }
                    if (generalOidcUpParty.Form.Model.Profiles?.Count() > 0)
                    {
                        foreach (var profile in generalOidcUpParty.Form.Model.Profiles)
                        {
                            if (!profile.InitName.IsNullOrWhiteSpace() && profile.InitName != profile.Name)
                            {
                                var profileMap = oidcUpParty.Profiles?.Where(p => p.Name == profile.Name).First();
                                profileMap.Name = profile.InitName;
                                profileMap.NewName = profile.Name;
                            }
                        }
                    }

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
                    generalOidcUpParty.Name = oidcUpPartyResult.Name;
                    if (deleteClientSecret)
                    {
                        await UpPartyService.DeleteOidcClientSecretUpPartyAsync(UpParty.Name);
                        oidcUpPartyResult.Client.ClientSecret = null;
                    }
                    var clientKeyResponse = await UpPartyService.GetOidcClientKeyUpPartyAsync(UpParty.Name);
                    generalOidcUpParty.Form.UpdateModel(ToViewModel(generalOidcUpParty, oidcUpPartyResult, clientKeyResponse));
                    toastService.ShowSuccess("OpenID Connect application updated.");
                    generalOidcUpParty.DisplayName = oidcUpPartyResult.DisplayName;
                    generalOidcUpParty.Profiles = oidcUpPartyResult.Profiles?.Map<List<UpPartyProfile>>();
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

        private async Task OnImportClientKeyFileAsync(GeneralOidcUpPartyViewModel oidcUpParty, InputFileChangeEventArgs e)
        {
            try
            {
                importClientKeyForm.ClearFieldError(nameof(importClientKeyForm.Model.ClientKeyFileStatus));
                
                if (e.File.Size > GeneralTrackCertificateViewModel.CertificateMaxFileSize)
                {
                    importClientKeyForm.SetFieldError(nameof(importClientKeyForm.Model.ClientKeyFileStatus), $"That's too big. Max size: {GeneralTrackCertificateViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                importClientKeyForm.Model.ClientKeyFileStatus = "Loading...";

                byte[] certificateBytes;
                using (var memoryStream = new MemoryStream())
                {
                    using var fileStream = e.File.OpenReadStream();
                    await fileStream.CopyToAsync(memoryStream);
                    certificateBytes = memoryStream.ToArray();
                }

                var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(certificateBytes);
                var clientKeyResponse = await UpPartyService.CreateOidcClientKeyUpPartyAsync(new OAuthClientKeyRequest { PartyName = UpParty.Name, Certificate = base64UrlEncodeCertificate, Password = importClientKeyForm.Model.Password });

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
