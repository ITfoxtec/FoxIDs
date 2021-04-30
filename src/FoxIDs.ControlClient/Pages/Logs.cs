using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
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
        private List<string> queryTypeItems = new List<string> { LogQueryTypes.Exceptions, LogQueryTypes.Events, LogQueryTypes.Traces, LogQueryTypes.Metrics };

        private string logLoadError;
        private PageEditForm<LogRequestViewModel> logRequestForm;
        private LogResponseViewModel logResponse;
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
                logLoadError = ex.Message;
            }
        }

        private async Task LoadLogAsync()
        {
            logLoadError = null;
            var fromTime = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(logRequestForm?.Model?.FromTime))
            {
                try
                {
                    fromTime = DateTimeOffset.Parse(logRequestForm.Model.FromTime);
                }
                catch (Exception tex)
                {
                    logRequestForm.SetFieldError(nameof(logRequestForm.Model.FromTime), tex.Message);
                    return;
                }
            }

            var logRequest = new LogRequest();
            logRequest.FromTime = fromTime.ToUnixTimeSeconds();
            logRequest.ToTime = fromTime.AddSeconds((int)(logRequestForm?.Model != null ? logRequestForm.Model.TimeInterval : LogTimeIntervals.FifteenMinutes)).ToUnixTimeSeconds();
            if (logRequestForm?.Model != null)
            {
                logRequest.Filter = logRequestForm.Model.Filter;
                logRequest.QueryExceptions = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Exceptions);
                logRequest.QueryTraces = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Traces);
                logRequest.QueryEvents = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Events);
                logRequest.QueryMetrics = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Metrics);
            }

            logResponse = (await TrackService.GetTrackLogAsync(logRequest)).Map<LogResponseViewModel>();
        }

        private async Task OnLogRequestValidSubmitAsync(EditContext editContext)
        {
            if (logRequestForm.Model.QueryTypes.Count() <= 0)
            {
                logRequestForm.Model.QueryTypes.Add(LogQueryTypes.Exceptions);
                logRequestForm.Model.QueryTypes.Add(LogQueryTypes.Events);
            }

            await LoadLogAsync();
        }
    }
}
