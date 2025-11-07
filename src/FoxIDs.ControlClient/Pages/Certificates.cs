using FoxIDs.Infrastructure;
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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using FoxIDs.Client.Models.Config;
using Microsoft.JSInterop;

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
        public ClientSettings clientSettings { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }        

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

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

        protected override void OnDispose()
        {
            TrackSelectedLogic.OnTrackSelectedAsync -= OnTrackSelectedAsync;
            base.OnDispose();
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

        private async Task SelectContainerTypeAsync(TrackKeyTypes type)
        {
            changeContainerTypeError = null;
            try
            {
                await TrackService.UpdateTrackKeyTypeAsync(new TrackKey { Type = type }, cancellationToken: PageCancellationToken);
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

        private async Task DownloadCertificateAsync(string subject, string certificateBase64)
        {
            if (certificateBase64.IsNullOrWhiteSpace())
            {
                return;
            }

            await JSRuntime.InvokeAsync<object>("saveCertFile", $"{subject}.cer", certificateBase64);
        }

        private async Task DefaultLoadAsync()
        {
            certificateLoadError = null;
            try
            {
                trackKey = await TrackService.GetTrackKeyTypeAsync(cancellationToken: PageCancellationToken);

                if(trackKey.Type == TrackKeyTypes.Contained || trackKey.Type == TrackKeyTypes.ContainedRenewSelfSigned)
                {
                    var trackKeys = await TrackService.GetTrackKeyContainedAsync(cancellationToken: PageCancellationToken);
                    SetGeneralCertificates(trackKeys, trackKey.Type == TrackKeyTypes.Contained);
                }
                else
                {
                    certificates.Clear();
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                trackKey = null;
                certificates?.Clear();
                certificateLoadError = ex.Message;
            }
        }

        private void SetGeneralCertificates(TrackKeyItemsContained trackKeys, bool includeCreatePlaceholder)
        {
            certificates.Clear();
            if (trackKeys?.PrimaryKey != null)
            {
                certificates.Add(new GeneralTrackCertificateViewModel(trackKeys.PrimaryKey, true));
            }

            if(trackKeys?.SecondaryKey != null)
            {
                certificates.Add(new GeneralTrackCertificateViewModel(trackKeys.SecondaryKey, false));
            }
            else if (includeCreatePlaceholder)
            {
                certificates.Add(new GeneralTrackCertificateViewModel(false) { CreateMode = true });
            }
        }

        private async Task ShowSwapCertificateAsync()
        {
            swapCertificateError = null;
            try
            {
                await TrackService.SwapTrackKeyContainedAsync(new TrackKeyItemContainedSwap { SwapKeys = true }, cancellationToken: PageCancellationToken);
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
                model.CertificateBase64 = generalCertificate.CertificateBase64;
            }
        }

        private void CertificateCancel(GeneralTrackCertificateViewModel generalCertificate)
        {
            generalCertificate.Edit = false;
        }

        private async Task OnCertificateFileSelectedAsync(GeneralTrackCertificateViewModel generalCertificate, InputFileChangeEventArgs e)
        {
            try
            {
                generalCertificate.Form.ClearFieldError(nameof(generalCertificate.Form.Model.Key));
                if (e.File.Size > GeneralTrackCertificateViewModel.CertificateMaxFileSize)
                {
                    generalCertificate.Form.SetFieldError(nameof(generalCertificate.Form.Model.Key), $"That's too big. Max size: {GeneralTrackCertificateViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalCertificate.CertificateFileStatus = "Loading...";

                byte[] certificateBytes;
                using (var memoryStream = new MemoryStream())
                {
                    using var fileStream = e.File.OpenReadStream();
                    await fileStream.CopyToAsync(memoryStream);
                    certificateBytes = memoryStream.ToArray();
                }

                var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(certificateBytes);
                var jwkWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate, Password = generalCertificate.Form.Model.Password }, cancellationToken: PageCancellationToken);
                    
                if (!jwkWithCertificateInfo.HasPrivateKey())
                {
                    generalCertificate.Form.Model.Subject = null;
                    generalCertificate.Form.Model.Key = null;
                    generalCertificate.Form.SetFieldError(nameof(generalCertificate.Form.Model.Key), "Private key is required. Maybe a password is required to unlock the private key.");
                    generalCertificate.CertificateFileStatus = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;
                    return;
                }

                generalCertificate.Form.Model.Subject = jwkWithCertificateInfo.CertificateInfo.Subject;
                generalCertificate.Form.Model.ValidFrom = jwkWithCertificateInfo.CertificateInfo.ValidFrom;
                generalCertificate.Form.Model.ValidTo = jwkWithCertificateInfo.CertificateInfo.ValidTo;
                generalCertificate.Form.Model.IsValid = jwkWithCertificateInfo.CertificateInfo.IsValid();
                generalCertificate.Form.Model.Thumbprint = jwkWithCertificateInfo.CertificateInfo.Thumbprint;
                generalCertificate.Form.Model.Key = jwkWithCertificateInfo;
                generalCertificate.Form.Model.CertificateBase64 = jwkWithCertificateInfo.X5c?.FirstOrDefault();
                generalCertificate.CertificateBase64 = generalCertificate.Form.Model.CertificateBase64;
                generalCertificate.Key = jwkWithCertificateInfo;
                generalCertificate.KeyId = jwkWithCertificateInfo.Kid;

                generalCertificate.CertificateFileStatus = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalCertificate.CertificateFileStatus = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;
                generalCertificate.Form.SetFieldError(nameof(generalCertificate.Form.Model.Key), ex.Message);
            }
            catch (FoxIDsApiException aex)
            {
                generalCertificate.CertificateFileStatus = GeneralTrackCertificateViewModel.DefaultCertificateFileStatus;
                generalCertificate.Form.SetFieldError(nameof(generalCertificate.Form.Model.Key), aex.Message);
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
                }), cancellationToken: PageCancellationToken);

                var keyResponse = generalCertificate.Form.Model.IsPrimary ? trackKeyResponse.PrimaryKey : trackKeyResponse.SecondaryKey;

                generalCertificate.Subject = keyResponse.CertificateInfo.Subject;
                generalCertificate.ValidFrom = keyResponse.CertificateInfo.ValidFrom;
                generalCertificate.ValidTo = keyResponse.CertificateInfo.ValidTo;
                generalCertificate.IsValid = keyResponse.CertificateInfo.IsValid();
                generalCertificate.Thumbprint = keyResponse.CertificateInfo.Thumbprint;
                generalCertificate.CertificateBase64 = keyResponse.X5c?.FirstOrDefault();
                generalCertificate.Key = keyResponse;
                generalCertificate.KeyId = keyResponse.Kid;
                if (generalCertificate.Form?.Model != null)
                {
                    generalCertificate.Form.Model.CertificateBase64 = generalCertificate.CertificateBase64;
                    generalCertificate.Form.Model.Key = keyResponse;
                    generalCertificate.Form.Model.KeyId = keyResponse.Kid;
                }
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
                    throw new Exception("Please add the certificate.");
                }

                _ = await TrackService.UpdateTrackKeyContainedAsync(generalCertificate.Form.Model.Map<TrackKeyItemContainedRequest>(), cancellationToken: PageCancellationToken);
                generalCertificate.Subject = generalCertificate.Form.Model.Subject;
                generalCertificate.ValidFrom = generalCertificate.Form.Model.ValidFrom;
                generalCertificate.ValidTo = generalCertificate.Form.Model.ValidTo;
                generalCertificate.IsValid = generalCertificate.Form.Model.IsValid;
                generalCertificate.Thumbprint = generalCertificate.Form.Model.Thumbprint;
                generalCertificate.CertificateBase64 = generalCertificate.Form.Model.CertificateBase64;
                generalCertificate.Key = generalCertificate.Form.Model.Key;
                generalCertificate.KeyId = generalCertificate.Form.Model.Key?.Kid;
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
                    generalCertificate.Form.SetError(ex.Message);
                }
            }
            catch (Exception ex)
            {
                generalCertificate.Form.SetError(ex.Message);
            }
        }

        private async Task DeleteSecondaryCertificateAsync(GeneralTrackCertificateViewModel generalCertificate)
        {
            try
            {
                await TrackService.DeleteTrackKeyContainedAsync(cancellationToken: PageCancellationToken);
                generalCertificate.CreateMode = true;
                generalCertificate.Edit = false; 
                generalCertificate.Subject = null;
                generalCertificate.Form.Model.Subject = null;
                generalCertificate.CertificateBase64 = null;
                generalCertificate.Key = null;
                generalCertificate.KeyId = null;
                if (generalCertificate.Form?.Model != null)
                {
                    generalCertificate.Form.Model.CertificateBase64 = null;
                    generalCertificate.Form.Model.Key = null;
                    generalCertificate.Form.Model.KeyId = null;
                }
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
