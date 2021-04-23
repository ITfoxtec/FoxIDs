using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class Logs
    {
        private PageEditForm<LogRequestViewModel> logRequestForm;
        private LogResponse logResponse;
        private string logSettingsHref;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            logSettingsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logsettings";
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
            try
            {
                await LoadLogAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                logRequestForm.SetError(ex.Message);
            }
        }

        private async Task LoadLogAsync()
        {
            DateTimeOffset? startingTime = null;
            if (!string.IsNullOrWhiteSpace(logRequestForm?.Model?.StartingTime))
            {
                try
                {
                    startingTime = DateTimeOffset.Parse(logRequestForm.Model.StartingTime);
                }
                catch (Exception tex)
                {
                    logRequestForm.SetFieldError(nameof(logRequestForm.Model.StartingTime), tex.Message);
                    return;
                }
            }

            var logRequest = new LogRequest();
            //var logRequest = logRequestForm == null ? new LogRequest() : logRequestForm.Map<LogRequest>(afterMap: afterMap =>
            //{
            //    if (startingTime.HasValue)
            //    {
            //        afterMap.StartingTime = startingTime.Value.ToUnixTimeSeconds();
            //    }
            //    else
            //    {
            //        afterMap.StartingTime = null;
            //    }
            //});

            logResponse = await TrackService.GetTrackLogAsync(logRequest);
        }

        private async Task OnLogRequestValidSubmitAsync(EditContext editContext)
        {
            await LoadLogAsync();
        }
    }
}
