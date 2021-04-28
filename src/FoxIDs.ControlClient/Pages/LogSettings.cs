using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class LogSettings
    {
        private string logsHref;
        private GeneralLogSettingsViewModel generalLogSettings = new GeneralLogSettingsViewModel();

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            logsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logs";
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

        private async Task DefaultLoadAsync()
        {
        //    certificateLoadError = null;
        //    try
        //    {
        //        trackKey = await TrackService.GetTrackLogSettingsAsync();

        //        if (trackKey.Type == TrackKeyType.Contained)
        //        {
        //            SetGeneralCertificates(await TrackService.GetTrackKeyContainedAsync());
        //        }
        //    }
        //    catch (TokenUnavailableException)
        //    {
        //        await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        certificateLoadError = ex.Message;
        //    }
        }

        private async Task ShowUpdateLogSettingsAsync()
        {
            generalLogSettings.Error = null;
            generalLogSettings.Edit = true;

            try
            {
                var logSettings = await TrackService.GetTrackLogSettingAsync();
                await generalLogSettings.Form.InitAsync(logSettings);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalLogSettings.Error = ex.Message;
            }
        }

        private async Task OnUpdateLogSettingsValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TrackService.SaveTrackLogSettingAsync(generalLogSettings.Form.Model);
                generalLogSettings.Edit = false;
            }
            catch (Exception ex)
            {
                generalLogSettings.Form.SetError(ex.Message);
            }
        }

    }
}
