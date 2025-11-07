using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Client.Shared;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using ITfoxtec.Identity;
using Blazored.Toast.Services;
using FoxIDs.Client.Models.Config;
using Microsoft.AspNetCore.WebUtilities;
using System.IO;

namespace FoxIDs.Client.Pages.Settings
{
    public partial class SmsSettings : PageBase
    {
        const string DefaultFileStatus = "Drop certificate file here or click to select";
        private string trackSettingsHref;
        private string mailSettingsHref;
        private string claimMappingsHref;
        private string textsHref;
        private string plansHref;
        private string smsPricesHref;
        private string riskPasswordsHref;
        private PageEditForm<SmsSettingsViewModel> smsSettingsForm;
        private Modal certificateModal; // @ref from SmsSettings.razor
        private string certificateError;
        private JwkWithCertificateInfo stagedKey; // staged certificate selection in modal
        private string deleteSmsError;
        private bool deleteSmsAcknowledge = false;
        private string certificateSource = "pfx"; // pfx or pem
        private string pfxPassword = DefaultFileStatus;
        private byte[] pfxBytes;
        private string pfxFileStatus;
        private string pemCrt;
        private string pemKey;
        private string pemCrtFileStatus = DefaultFileStatus;
        private string pemKeyFileStatus = DefaultFileStatus;


        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        protected override async Task OnInitializedAsync()
        {
            trackSettingsHref = $"{TenantName}/envsettings";
            mailSettingsHref = $"{TenantName}/mailsettings";
            claimMappingsHref = $"{TenantName}/claimmappings";
            textsHref = $"{TenantName}/texts";
            plansHref = $"{TenantName}/plans";
            smsPricesHref = $"{TenantName}/smsprices";
            riskPasswordsHref = $"{TenantName}/riskpasswords";
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

        private async Task DefaultLoadAsync()
        {
            smsSettingsForm?.ClearError();
            try
            {
                var smsSettings = await TrackService.GetTrackSendSmsAsync(cancellationToken: PageCancellationToken);
                if (smsSettings == null)
                {
                    smsSettings = new SendSms();
                }
                await smsSettingsForm.InitAsync(new SmsSettingsViewModel
                {
                    Type = smsSettings.Type,
                    FromName = smsSettings.FromName,
                    ApiUrl = smsSettings.ApiUrl,
                    ClientId = smsSettings.ClientId,
                    ClientSecret = smsSettings.ClientSecret,
                    Label = smsSettings.Label,
                    Key = smsSettings.Key,
                });
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                smsSettingsForm.SetError(ex.Message);
            }
        }

        private async Task OnUpdateSmsValidSubmitAsync(EditContext editContext)
        {
            try
            {

                switch (smsSettingsForm.Model.Type)
                {
                    case SendSmsTypes.GatewayApi:
                        smsSettingsForm.Model.ClientId = null;
                        smsSettingsForm.Model.Key = null;
                        if (smsSettingsForm.Model.Label.IsNullOrWhiteSpace())
                        {
                            smsSettingsForm.Model.Label = null;
                        }
                        else
                        {
                            smsSettingsForm.Model.Label = smsSettingsForm.Model.Label.Trim();
                        }
                        break;
                    case SendSmsTypes.Smstools:
                        smsSettingsForm.Model.Key = null;
                        smsSettingsForm.Model.Label = null;
                        break;
                    case SendSmsTypes.TeliaSmsGateway:
                        smsSettingsForm.Model.Label = null;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                await TrackService.UpdateTrackSendSmsAsync(smsSettingsForm.Model.Map<SendSms>(), cancellationToken: PageCancellationToken);
                toastService.ShowSuccess("SMS settings updated.");
            }
            catch (Exception ex)
            {
                smsSettingsForm.SetError(ex.Message);
            }
        }

        private async Task DeleteSmsAsync()
        {
            try
            {
                await TrackService.DeleteTrackSendSmsAsync(cancellationToken: PageCancellationToken);
                deleteSmsAcknowledge = false;
                await DefaultLoadAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                deleteSmsError = ex.Message;
            }
        }

        private async Task OnPfxSelectedAsync(InputFileChangeEventArgs e)
        {
            certificateError = null;
            pfxFileStatus = DefaultFileStatus;
            pfxBytes = null;
            var file = e.File;
            if (file == null) return;
            using var ms = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(ms);
            pfxBytes = ms.ToArray();
            pfxFileStatus = file.Name;
        }

        private async Task ConvertPfxToJwkAsync()
        {
            certificateError = null;
            try
            {
                smsSettingsForm?.ClearFieldError(nameof(smsSettingsForm.Model.Key));
                if (pfxBytes == null)
                {
                    smsSettingsForm.SetFieldError(nameof(smsSettingsForm.Model.Key), "Select a PFX file first.");
                    return;
                }
                var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(pfxBytes);
                stagedKey = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate, Password = pfxPassword }, cancellationToken: PageCancellationToken);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                certificateError = ex.Message;
            }
        }

        private async Task OnPemCrtSelectedAsync(InputFileChangeEventArgs e)
        {
            certificateError = null;
            pemCrtFileStatus = DefaultFileStatus;
            pemCrt = null;
            var file = e.File;
            if (file == null) return;
            using var reader = new StreamReader(file.OpenReadStream());
            pemCrt = await reader.ReadToEndAsync();
            pemCrtFileStatus = file.Name;
        }

        private async Task OnPemKeySelectedAsync(InputFileChangeEventArgs e)
        {
            certificateError = null;
            pemKeyFileStatus = DefaultFileStatus;
            pemKey = null;
            var file = e.File;
            if (file == null) return;
            using var reader = new StreamReader(file.OpenReadStream());
            pemKey = await reader.ReadToEndAsync();
            pemKeyFileStatus = file.Name;
        }

        private async Task ConvertPemToJwkAsync()
        {
            certificateError = null;
            try
            {
                smsSettingsForm?.ClearFieldError(nameof(smsSettingsForm.Model.Key));
                if (pemCrt.IsNullOrWhiteSpace() || pemKey.IsNullOrWhiteSpace())
                {
                    smsSettingsForm.SetFieldError(nameof(smsSettingsForm.Model.Key), "Select both .crt and .key files.");
                    return;
                }
                stagedKey = await HelpersService.ReadCertificateFromPemAsync(new CertificateCrtAndKey { CertificatePemCrt = pemCrt, CertificatePemKey = pemKey }, cancellationToken: PageCancellationToken);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                certificateError = ex.Message;
            }
        }

        private void OpenCertificateModal()
        {
            certificateError = null;
            stagedKey = null;
            // Reset file inputs/status
            certificateSource = "pfx";
            pfxPassword = null;
            pfxBytes = null;
            pfxFileStatus = DefaultFileStatus;
            pemCrt = null;
            pemKey = null;
            pemCrtFileStatus = DefaultFileStatus;
            pemKeyFileStatus = DefaultFileStatus;
            certificateModal?.Show();
        }

        private void ConfirmCertificateSelection()
        {
            smsSettingsForm?.ClearFieldError(nameof(smsSettingsForm.Model.Key));
            if (stagedKey != null)
            {
                smsSettingsForm.Model.Key = stagedKey;
            }
            certificateModal?.Hide();
        }

        private void CancelCertificateSelection()
        {
            // Discard staged changes
            stagedKey = null;
            // And close modal
            certificateModal?.Hide();
        }
    }
}
