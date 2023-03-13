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
using Microsoft.AspNetCore.Components;
using Tewr.Blazor.FileReader;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ESamlUpParty : UpPartyBase
    {
        private ElementReference readMetadataFileElement;

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Inject]
        public IFileReaderService fileReaderService { get; set; }

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
                var generalSamlUpParty = UpParty as GeneralSamlUpPartyViewModel;
                var samlUpParty = await UpPartyService.GetSamlUpPartyAsync(UpParty.Name);
                await generalSamlUpParty.Form.InitAsync(ToViewModel(generalSamlUpParty, samlUpParty));
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

        private SamlUpPartyViewModel ToViewModel(GeneralSamlUpPartyViewModel generalSamlUpParty, SamlUpParty samlUpParty)
        {
            return samlUpParty.Map<SamlUpPartyViewModel>(afterMap =>
            {
                if (samlUpParty.UpdateState == PartyUpdateStates.Manual)
                {
                    afterMap.IsManual = true;
                }

                if (samlUpParty.UpdateState == PartyUpdateStates.AutomaticStopped)
                {
                    afterMap.AutomaticStopped = true;
                }
                else
                {
                    afterMap.AutomaticStopped = false;
                }

                afterMap.EnableSingleLogout = !samlUpParty.DisableSingleLogout;

                if (samlUpParty.AuthnContextComparison.HasValue)
                {
                    afterMap.AuthnContextComparisonViewModel = (SamlAuthnContextComparisonTypesVievModel)Enum.Parse(typeof(SamlAuthnContextComparisonTypesVievModel), samlUpParty.AuthnContextComparison.Value.ToString());
                }
                else
                {
                    afterMap.AuthnContextComparisonViewModel = SamlAuthnContextComparisonTypesVievModel.Null;
                }

                generalSamlUpParty.KeyInfoList.Clear();
                foreach (var key in afterMap.Keys)
                {
                    generalSamlUpParty.KeyInfoList.Add(new KeyInfoViewModel
                    {
                        Subject = key.CertificateInfo.Subject,
                        ValidFrom = key.CertificateInfo.ValidFrom,
                        ValidTo = key.CertificateInfo.ValidTo,
                        IsValid = key.CertificateInfo.IsValid(),
                        Thumbprint = key.CertificateInfo.Thumbprint,
                        Key = key
                    });
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void SamlUpPartyViewModelAfterInit(GeneralSamlUpPartyViewModel samlUpParty, SamlUpPartyViewModel model)
        {
            if (samlUpParty.CreateMode)
            {
                model.Claims = new List<string> { "*" };
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

                string metadataXml;
                await using (var stream = await file.OpenReadAsync())
                {
                    byte[] resultBytes = new byte[stream.Length];
                    await stream.ReadAsync(resultBytes, 0, (int)stream.Length);

                    metadataXml = Encoding.ASCII.GetString(resultBytes);
                }

                var samlUpParty = await UpPartyService.ReadSamlUpPartyMetadataAsync(new SamlReadMetadataRequest { Type = SamlReadMetadataType.Xml, Metadata = metadataXml });

                generalSamlUpParty.Form.Model.Issuer = samlUpParty.Issuer;
                generalSamlUpParty.Form.Model.AuthnUrl = samlUpParty.AuthnUrl;
                if (samlUpParty.AuthnRequestBinding.HasValue)
                {
                    generalSamlUpParty.Form.Model.AuthnRequestBinding = samlUpParty.AuthnRequestBinding.Value;
                }

                generalSamlUpParty.Form.Model.LogoutUrl = samlUpParty.LogoutUrl;
                if (!string.IsNullOrEmpty(samlUpParty.SingleLogoutResponseUrl))
                {
                    generalSamlUpParty.Form.Model.SingleLogoutResponseUrl = samlUpParty.SingleLogoutResponseUrl;
                }
                if (samlUpParty.LogoutRequestBinding.HasValue)
                {
                    generalSamlUpParty.Form.Model.LogoutRequestBinding = samlUpParty.LogoutRequestBinding.Value;
                }
    
                generalSamlUpParty.KeyInfoList = new List<KeyInfoViewModel>();
                generalSamlUpParty.Form.Model.Keys = new List<JwtWithCertificateInfo>();

                if (samlUpParty.Keys?.Count() > 0)
                {
                    foreach(var key in samlUpParty.Keys)
                    {
                        generalSamlUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = key.CertificateInfo.Subject,
                            ValidFrom = key.CertificateInfo.ValidFrom,
                            ValidTo = key.CertificateInfo.ValidTo,
                            IsValid = key.CertificateInfo.IsValid(),
                            Thumbprint = key.CertificateInfo.Thumbprint,
                            Key = key
                        });
                        generalSamlUpParty.Form.Model.Keys.Add(key);
                    }
                }

                generalSamlUpParty.Form.Model.SignAuthnRequest = samlUpParty.SignAuthnRequest;
            }
            catch (Exception ex)
            {
                generalSamlUpParty.Form.SetError($"Failing SAML 2.0 metadata. {ex.Message}");
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
                    var samlUpPartyResult = await UpPartyService.CreateSamlUpPartyAsync(samlUpParty);
                    generalSamlUpParty.Form.UpdateModel(ToViewModel(generalSamlUpParty, samlUpPartyResult));
                    generalSamlUpParty.CreateMode = false;
                    toastService.ShowSuccess("SAML 2.0 up-party created.");
                }
                else
                {
                    var samlUpPartyResult = await UpPartyService.UpdateSamlUpPartyAsync(samlUpParty);
                    generalSamlUpParty.Form.UpdateModel(ToViewModel(generalSamlUpParty, samlUpPartyResult));
                    toastService.ShowSuccess("SAML 2.0 up-party updated.");
                }
                generalSamlUpParty.Name = generalSamlUpParty.Form.Model.Name;
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
