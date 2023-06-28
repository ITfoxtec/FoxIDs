using Blazored.Toast.Services;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class TextSettings
    {
        private string textsHref;
        private GeneralResourceSettingsViewModel generalTextSettings = new GeneralResourceSettingsViewModel();

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            textsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/texts";
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

        private Task DefaultLoadAsync()
        {
            generalTextSettings.Error = null;
            generalTextSettings.Edit = false;
            return Task.CompletedTask;
        }

        private async Task ShowUpdateTextSettingsAsync()
        {
            generalTextSettings.Error = null;
            generalTextSettings.Edit = true;

            try
            {
                var textSettings = await TrackService.GetTrackResourceSettingAsync();
                await generalTextSettings.Form.InitAsync(textSettings);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalTextSettings.Error = ex.Message;
            }
        }

        private async Task OnUpdateTextSettingsValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TrackService.SaveTrackResourceSettingAsync(generalTextSettings.Form.Model);
                generalTextSettings.Edit = false;
                toastService.ShowSuccess("Text settings updated.");
            }
            catch (Exception ex)
            {
                generalTextSettings.Form.SetError(ex.Message);
            }
        }
    }
}
