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
using ITfoxtec.Identity.Models;
using System.Text.Json;

namespace FoxIDs.Client.Pages.Settings
{
    public partial class SmsSettings : PageBase
    {
        private string trackSettingsHref;
        private string mailSettingsHref;
        private string claimMappingsHref;
        private string textsHref;
        private string plansHref;
        private string smsPricesHref;
        private string riskPasswordsHref;
        private PageEditForm<SmsSettingsViewModel> smsSettingsForm;
        private string deleteSmsError;
        private bool deleteSmsAcknowledge = false;

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

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
                var smsSettings = await TrackService.GetTrackSendSmsAsync();
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
                    KeyJson = smsSettings.Key != null ? JsonSerializer.Serialize(smsSettings.Key, new JsonSerializerOptions { WriteIndented = true }) : null
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
        JsonWebKey jwk = null;
                if (smsSettingsForm.Model.Type == SendSmsTypes.TeliaSmsGateway)
                {
                    if (smsSettingsForm.Model.KeyJson.IsNullOrWhiteSpace())
                    {
                        smsSettingsForm.SetFieldError(nameof(smsSettingsForm.Model.KeyJson), "mTLS certificate JWK is required.");
                        return;
                    }
                    try
                    {
            jwk = JsonSerializer.Deserialize<JsonWebKey>(smsSettingsForm.Model.KeyJson);
                    }
                    catch (Exception)
                    {
                        smsSettingsForm.SetFieldError(nameof(smsSettingsForm.Model.KeyJson), "Invalid JWK JSON.");
                        return;
                    }
                }

                var payload = new SendSms
                {
                    Type = smsSettingsForm.Model.Type,
                    FromName = smsSettingsForm.Model.FromName,
                    ApiUrl = smsSettingsForm.Model.ApiUrl,
                    ClientId = smsSettingsForm.Model.ClientId,
                    ClientSecret = smsSettingsForm.Model.ClientSecret,
                    Key = jwk
                };
                await TrackService.UpdateTrackSendSmsAsync(payload);
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
                await TrackService.DeleteTrackSendSmsAsync();
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
    }
}
