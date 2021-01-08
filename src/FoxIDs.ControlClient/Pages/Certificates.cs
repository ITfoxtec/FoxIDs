using BlazorInputFile;
using FoxIDs.Client.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MTokens = Microsoft.IdentityModel.Tokens;

namespace FoxIDs.Client.Pages
{
    public partial class Certificates
    {
        private Modal swapCertificateModal;
        private string swapCertificateError;
        private List<GeneralTrackCertificateViewModel> certificates = new List<GeneralTrackCertificateViewModel>();
        private Modal changeContainerTypeModal;
        private string changeContainerTypeError;
        private string certificateLoadError;
        private TrackKey trackKey;

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task OnTrackSelectedAsync(Track track)
        {
            await DefaultLoadAsync();
            StateHasChanged();
        }

        private void ShowContainerTypeAsync()
        {
            changeContainerTypeError = null;
            changeContainerTypeModal.Show();
        }

        private async Task SelectContainerTypeAsync(TrackKeyType type)
        {
            changeContainerTypeError = null;
            try
            {
                await TrackService.UpdateTrackKeyTypeAsync(new TrackKey { Type = type });
                trackKey.Type = type;
                changeContainerTypeModal.Hide();
                await DefaultLoadAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                changeContainerTypeError = ex.Message;
            }
        }

        private async Task DefaultLoadAsync()
        {
            certificateLoadError = null;
            try
            {
                trackKey = await TrackService.GetTrackKeyTypeAsync();

                if(trackKey.Type == TrackKeyType.Contained)
                {
                    SetGeneralCertificates(await TrackService.GetTrackKeyContainedAsync());
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                certificateLoadError = ex.Message;
            }
        }

        private void SetGeneralCertificates(TrackKeyItemsContained trackKeys)
        {
            certificates.Clear();
            certificates.Add(new GeneralTrackCertificateViewModel(trackKeys.PrimaryKey, true));
            if(trackKeys.SecondaryKey != null)
            {
                certificates.Add(new GeneralTrackCertificateViewModel(trackKeys.SecondaryKey, false));
            }
            else
            {
                certificates.Add(new GeneralTrackCertificateViewModel(false) { CreateMode = true });
            }
        }

        private async Task ShowSwapCertificateAsync()
        {
            swapCertificateError = null;
            try
            {
                await TrackService.SwapTrackKeyContainedAsync(new TrackKeyItemContainedSwap { SwapKeys = true });
                await DefaultLoadAsync();
                swapCertificateModal.Hide();
            }
            catch (TokenUnavailableException)
            {
                await(OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                swapCertificateError = ex.Message;
            }
        }

        private void ShowCreateSecondaryCertificate(GeneralTrackCertificateViewModel generalCertificate)
        {
            generalCertificate.CreateMode = true;
            generalCertificate.DeleteAcknowledge = false;
            generalCertificate.ShowAdvanced = false;
            generalCertificate.Error = null;
            generalCertificate.Edit = true;
        }

        private void ShowUpdateCertificate(GeneralTrackCertificateViewModel generalCertificate)
        {
            generalCertificate.CreateMode = false;
            generalCertificate.DeleteAcknowledge = false;
            generalCertificate.ShowAdvanced = false;
            generalCertificate.Error = null;
            generalCertificate.Edit = true;
        }


        private void CertificateViewModelAfterInit(GeneralTrackCertificateViewModel generalCertificate, TrackCertificateInfoViewModel model)
        {
            model.IsPrimary = generalCertificate.IsPrimary;

            if (generalCertificate.Edit)
            {
                model.Subject = generalCertificate.Subject;
                model.ValidFrom = generalCertificate.ValidFrom;
                model.ValidTo = generalCertificate.ValidTo;
                model.IsValid = generalCertificate.IsValid;
                model.Thumbprint = generalCertificate.Thumbprint;
            }
        }

        private void CertificateCancel(GeneralTrackCertificateViewModel generalCertificate)
        {
            generalCertificate.Edit = false;
        }

        private async Task OnCertificateFileSelectedAsync(GeneralTrackCertificateViewModel generalCertificate, IFileListEntry[] files)
        {
            generalCertificate.Form.ClearFieldError(nameof(generalCertificate.Form.Model.Key));
            foreach (var file in files)
            {
                if (file.Size > GeneralTrackCertificateViewModel.CertificateMaxFileSize)
                {
                    generalCertificate.Form.SetFieldError(nameof(generalCertificate.Form.Model.Key), $"That's too big. Max size: {GeneralTrackCertificateViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalCertificate.CertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    await file.Data.CopyToAsync(memoryStream);

                    try
                    {
                        var certificate = new X509Certificate2(memoryStream.ToArray());
                        var jwk = await certificate.ToFTJsonWebKeyAsync(true);
                        if (!jwk.HasPrivateKey())
                        {
                            generalCertificate.Form.SetFieldError(nameof(generalCertificate.Form.Model.Key), "Private key is required.");
                            return;
                        }                        

                        generalCertificate.Form.Model.Subject = certificate.Subject;
                        generalCertificate.Form.Model.ValidFrom = certificate.NotBefore;
                        generalCertificate.Form.Model.ValidTo = certificate.NotAfter;
                        generalCertificate.Form.Model.IsValid = certificate.IsValid();
                        generalCertificate.Form.Model.Thumbprint = certificate.Thumbprint;
                        generalCertificate.Form.Model.Key = jwk;
                    }
                    catch (Exception ex)
                    {
                        generalCertificate.Form.SetFieldError(nameof(generalCertificate.Form.Model.Key), ex.Message);
                    }
                }

                generalCertificate.CertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private async Task CreateSelfSignedCertificateAsync(GeneralTrackCertificateViewModel generalCertificate)
        {
            generalCertificate.Form.ClearError();
            try
            {
                var trackKeyResponse = await TrackService.UpdateTrackKeyContainedAsync(generalCertificate.Form.Model.Map<TrackKeyItemContainedRequest>(afterMap: afterMap => 
                {
                    afterMap.CreateSelfSigned = true;
                    afterMap.Key = null;
                }));
                var certificate = new MTokens.JsonWebKey((generalCertificate.Form.Model.IsPrimary ? trackKeyResponse.PrimaryKey : trackKeyResponse.SecondaryKey).JsonSerialize()).ToX509Certificate();
                generalCertificate.Subject = certificate.Subject;
                generalCertificate.ValidFrom = certificate.NotBefore;
                generalCertificate.ValidTo = certificate.NotAfter;
                generalCertificate.IsValid = certificate.IsValid();
                generalCertificate.Thumbprint = certificate.Thumbprint;
                generalCertificate.CreateMode = false;
                generalCertificate.Edit = false;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalCertificate.Form.SetError(ex.Message);
            }
            catch (FoxIDsApiException aex)
            {
                generalCertificate.Form.SetError(aex.Message);
            }
        }

        private async Task OnEditCertificateValidSubmitAsync(GeneralTrackCertificateViewModel generalCertificate, EditContext editContext)
        {
            try
            {
                if(generalCertificate.Form.Model.Key == null)
                {
                    throw new ArgumentNullException("Model.Key");
                }

                _ = await TrackService.UpdateTrackKeyContainedAsync(generalCertificate.Form.Model.Map<TrackKeyItemContainedRequest>());
                generalCertificate.Subject = generalCertificate.Form.Model.Subject;
                generalCertificate.ValidFrom = generalCertificate.Form.Model.ValidFrom;
                generalCertificate.ValidTo = generalCertificate.Form.Model.ValidTo;
                generalCertificate.IsValid = generalCertificate.Form.Model.IsValid;
                generalCertificate.Thumbprint = generalCertificate.Form.Model.Thumbprint;
                generalCertificate.CreateMode = false;
                generalCertificate.Edit = false;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalCertificate.Form.SetFieldError(nameof(generalCertificate.Form.Model.Key), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSecondaryCertificateAsync(GeneralTrackCertificateViewModel generalCertificate)
        {
            try
            {
                await TrackService.DeleteTrackKeyContainedAsync();
                generalCertificate.CreateMode = true;
                generalCertificate.Edit = false; 
                generalCertificate.Subject = null;
                generalCertificate.Form.Model.Subject = null;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalCertificate.Form.SetError(ex.Message);
            }
        }
    }
}
