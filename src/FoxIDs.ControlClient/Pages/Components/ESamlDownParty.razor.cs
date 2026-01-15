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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using Microsoft.JSInterop;
using System.Text;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ESamlDownParty : DownPartyBase
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        private KeyInfoViewModel IdPKeyInfo { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
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

        private void EnsureSamlDownPartySummaryDefaults(GeneralSamlDownPartyViewModel generalSamlDownParty)
        {
            var model = generalSamlDownParty?.Form?.Model;
            if (model == null)
            {
                return;
            }

            if (model.Name.IsNullOrWhiteSpace())
            {
                model.Metadata = null;
                model.MetadataIssuer = null;
                model.MetadataAuthn = null;
                model.MetadataLogout = null;
                generalSamlDownParty.ShowMetadataDetails = false;
                return;
            }

            (model.Metadata, model.MetadataIssuer, model.MetadataAuthn, model.MetadataLogout) = MetadataLogic.GetDownSamlMetadata(model.Name, model.PartyBindingPattern);
        }

        private async Task ShowSamlMetadataDetailsAsync(GeneralSamlDownPartyViewModel generalSamlDownParty)
        {
            if (generalSamlDownParty == null)
            {
                return;
            }

            if (IdPKeyInfo == null)
            {
                try
                {
                    var trackKeys = await TrackService.GetTrackKeyContainedAsync();
                    IdPKeyInfo = new KeyInfoViewModel
                    {
                        Subject = trackKeys.PrimaryKey.CertificateInfo.Subject,
                        ValidFrom = trackKeys.PrimaryKey.CertificateInfo.ValidFrom,
                        ValidTo = trackKeys.PrimaryKey.CertificateInfo.ValidTo,
                        IsValid = trackKeys.PrimaryKey.CertificateInfo.IsValid(),
                        Thumbprint = trackKeys.PrimaryKey.CertificateInfo.Thumbprint,
                        KeyId = trackKeys.PrimaryKey.Kid,
                        CertificateBase64 = trackKeys.PrimaryKey.X5c?.FirstOrDefault(),
                        Key = trackKeys.PrimaryKey
                    };
                }
                catch (TokenUnavailableException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                    return;
                }
                catch (Exception ex)
                {
                    generalSamlDownParty.Form.SetError(ex.Message);
                }
            }

            generalSamlDownParty.ShowMetadataDetails = true;
        }

        private async Task OnReadMetadataFileAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, InputFileChangeEventArgs e)
        {
            generalSamlDownParty.Form.ClearError();
            try
            {
                byte[] metadataXmlBytes;
                using (var memoryStream = new MemoryStream())
                {
                    using var fileStream = e.File.OpenReadStream();
                    await fileStream.CopyToAsync(memoryStream);
                    metadataXmlBytes = memoryStream.ToArray();
                }
                var metadataXml = Encoding.ASCII.GetString(metadataXmlBytes);

                var samlDownParty = await DownPartyService.ReadSamlDownPartyMetadataAsync(new SamlReadMetadataRequest { Type = SamlReadMetadataType.Xml, Metadata = metadataXml });

                generalSamlDownParty.Form.Model.Issuer = samlDownParty.Issuer;
                generalSamlDownParty.Form.Model.AcsUrls = samlDownParty.AcsUrls;
                if (samlDownParty.AuthnRequestBinding.HasValue)
                {
                    generalSamlDownParty.Form.Model.AuthnRequestBinding = samlDownParty.AuthnRequestBinding.Value;
                }
                if (samlDownParty.AuthnResponseBinding.HasValue)
                {
                    generalSamlDownParty.Form.Model.AuthnResponseBinding = samlDownParty.AuthnResponseBinding.Value;
                }

                generalSamlDownParty.Form.Model.SingleLogoutUrl = samlDownParty.SingleLogoutUrl;
                if (samlDownParty.LogoutRequestBinding.HasValue)
                {
                    generalSamlDownParty.Form.Model.LogoutRequestBinding = samlDownParty.LogoutRequestBinding.Value;
                }
                if (samlDownParty.LogoutResponseBinding.HasValue)
                {
                    generalSamlDownParty.Form.Model.LogoutResponseBinding = samlDownParty.LogoutResponseBinding.Value;
                }

                generalSamlDownParty.KeyInfoList = new List<KeyInfoViewModel>();
                generalSamlDownParty.Form.Model.Keys = new List<JwkWithCertificateInfo>();

                if (samlDownParty.Keys?.Count() > 0)
                {
                    foreach (var key in samlDownParty.Keys)
                    {
                        generalSamlDownParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = key.CertificateInfo.Subject,
                            ValidFrom = key.CertificateInfo.ValidFrom,
                            ValidTo = key.CertificateInfo.ValidTo,
                            Thumbprint = key.CertificateInfo.Thumbprint,
                            Key = key
                        });
                        generalSamlDownParty.Form.Model.Keys.Add(key);
                    }
                }
            }
            catch (Exception ex)
            {
                generalSamlDownParty.Form.SetError($"Failing SAML 2.0 metadata. {ex.Message}");
            }
        }

        private SamlDownPartyViewModel ToViewModel(GeneralSamlDownPartyViewModel generalSamlDownParty, SamlDownParty samlDownParty)
        {
            return samlDownParty.Map<SamlDownPartyViewModel>(afterMap =>
            {
                afterMap.InitName = afterMap.Name;

                if (afterMap.DisplayName.IsNullOrWhiteSpace())
                {
                    afterMap.DisplayName = afterMap.Name;
                }

                if (samlDownParty.UpdateState == PartyUpdateStates.Manual)
                {
                    afterMap.IsManual = true;
                }
                else
                {
                    afterMap.IsManual = false;
                }

                if (samlDownParty.UpdateState == PartyUpdateStates.AutomaticStopped)
                {
                    afterMap.AutomaticStopped = true;
                }
                else
                {
                    afterMap.AutomaticStopped = false;
                }

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
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapSamlClaimTransforms();
                }
            });
        }

        private async Task OnSamlDownPartyEncryptionCertificateFileSelectedAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, InputFileChangeEventArgs e)
        {
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.EncryptionKey));
            foreach (var file in e.GetMultipleFiles())
            {
                if (file.Size > GeneralSamlDownPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.EncryptionKey), $"That's too big. Max size: {GeneralSamlDownPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlDownParty.EncryptionCertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    using var fileStream = e.File.OpenReadStream();
                    await fileStream.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwkWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        generalSamlDownParty.EncryptionKeyInfo = new KeyInfoViewModel
                        {
                            Subject = jwkWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwkWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwkWithCertificateInfo.CertificateInfo.ValidTo,
                            Thumbprint = jwkWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwkWithCertificateInfo
                        };
                        generalSamlDownParty.Form.Model.EncryptionKey = jwkWithCertificateInfo;
                    }
                    catch (TokenUnavailableException)
                    {
                        await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
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


        private async Task OnSamlDownPartyCertificateFileSelectedAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, InputFileChangeEventArgs e)
        {
            if (generalSamlDownParty.Form.Model.Keys == null)
            {
                generalSamlDownParty.Form.Model.Keys = new List<JwkWithCertificateInfo>();
            }
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.Keys));
            foreach (var file in e.GetMultipleFiles())
            {
                if (file.Size > GeneralSamlDownPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), $"That's too big. Max size: {GeneralSamlDownPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlDownParty.CertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    using var fileStream = e.File.OpenReadStream();
                    await fileStream.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwkWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        if (generalSamlDownParty.Form.Model.Keys.Any(k => k.Kid.Equals(jwkWithCertificateInfo.Kid, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        generalSamlDownParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = jwkWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwkWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwkWithCertificateInfo.CertificateInfo.ValidTo,
                            Thumbprint = jwkWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwkWithCertificateInfo
                        });
                        generalSamlDownParty.Form.Model.Keys.Add(jwkWithCertificateInfo);
                    }
                    catch (TokenUnavailableException)
                    {
                        await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
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

        private async Task DownloadCertificateAsync(string subject, string certificateBase64)
        {
            if (certificateBase64.IsNullOrWhiteSpace())
            {
                return;
            }

            await JSRuntime.InvokeAsync<object>("saveCertFile", $"{subject}.cer", certificateBase64);
        }

        private async Task OnEditSamlDownPartyValidSubmitAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, EditContext editContext)
        {
            try
            {
                generalSamlDownParty.Form.Model.ClaimTransforms.MapSamlClaimTransformsBeforeMap();

                if(!generalSamlDownParty.Form.Model.EncryptAuthnResponse)
                {
                    generalSamlDownParty.Form.Model.EncryptionKey = null;
                }

                var samlDownParty = generalSamlDownParty.Form.Model.Map<SamlDownParty>(afterMap =>
                {
                    if (generalSamlDownParty.Form.Model.Name != generalSamlDownParty.Form.Model.InitName)
                    {
                        afterMap.NewName = afterMap.Name;
                        afterMap.Name = generalSamlDownParty.Form.Model.InitName;
                    }

                    if (generalSamlDownParty.Form.Model.IsManual)
                    {
                        afterMap.UpdateState = PartyUpdateStates.Manual;
                    }
                    else
                    {
                        afterMap.UpdateState = PartyUpdateStates.Automatic;
                    }

                    afterMap.ClaimTransforms.MapSamlClaimTransformsAfterMap();
                });

                var samlDownPartyResult = await DownPartyService.UpdateSamlDownPartyAsync(samlDownParty);
                generalSamlDownParty.Form.UpdateModel(ToViewModel(generalSamlDownParty, samlDownPartyResult));
                toastService.ShowSuccess("SAML 2.0 authentication method updated.");
                generalSamlDownParty.Name = samlDownPartyResult.Name;
                generalSamlDownParty.DisplayName = samlDownPartyResult.DisplayName;
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
