using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.Config;
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
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages.Logging
{
    public partial class Logs
    {
        private List<string> queryTypeItems = new List<string> { LogQueryTypes.Errors, LogQueryTypes.Warnings, LogQueryTypes.Events, LogQueryTypes.Traces, LogQueryTypes.Metrics };

        private string logLoadError;
        private PageEditForm<LogRequestViewModel> logRequestForm;
        private LogResponseViewModel logResponse;
        private string logUsageHref;
        private string logAuditHref;
        private string logSettingsHref;

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Inject]
        public MyTenantService MyTenantService { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        private bool IsMasterTrack => RouteBindingLogic.IsMasterTrack;

        protected override async Task OnInitializedAsync()
        {
            var tenantRouteName = await RouteBindingLogic.GetTenantNameAsync();
            logUsageHref = $"{tenantRouteName}/logusage";
            logAuditHref = $"{tenantRouteName}/logaudit";
            logSettingsHref = $"{tenantRouteName}/logsettings";
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            NotificationLogic.OnClientSettingLoaded += OnClientSettingLoaded;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        protected override void OnDispose()
        {
            TrackSelectedLogic.OnTrackSelectedAsync -= OnTrackSelectedAsync;
            NotificationLogic.OnClientSettingLoaded -= OnClientSettingLoaded;
            base.OnDispose();
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
                logResponse = null;
                logLoadError = ex.Message;
            }
        }

        private async Task LoadLogAsync()
        {
            logLoadError = null;

            if (IsMasterTenant)
            {
                var logRequest = new TenantLogRequest();
                AddLogValues(logRequest);
                if (logRequestForm?.Model != null)
                {
                    logRequest.TenantName = logRequestForm.Model.TenantName;
                    logRequest.TrackName = logRequestForm.Model.TrackName;
                }
                logResponse = (await TenantService.GetLogAsync(logRequest, cancellationToken: PageCancellationToken)).Map<LogResponseViewModel>();
            }
            else if (IsMasterTrack)
            {
                var logRequest = new MyTenantLogRequest();
                AddLogValues(logRequest);
                if (logRequestForm?.Model != null)
                {
                    logRequest.TrackName = logRequestForm.Model.TrackName;
                }
                logResponse = (await MyTenantService.GetLogAsync(logRequest, cancellationToken: PageCancellationToken)).Map<LogResponseViewModel>();
            }
            else
            {
                var logRequest = new LogRequest();
                AddLogValues(logRequest);
                logResponse = (await TrackService.GetLogAsync(logRequest, cancellationToken: PageCancellationToken)).Map<LogResponseViewModel>(); 
            }
        }

        private void AddLogValues(LogRequest logRequest)
        {
            var fromTime = GetFromTime();
            if (!fromTime.HasValue)
            {
                return;
            }
            logRequest.FromTime = fromTime.Value.ToUnixTimeSeconds();
            logRequest.ToTime = fromTime.Value.AddSeconds((int)(logRequestForm?.Model != null ? logRequestForm.Model.TimeInterval : LogTimeIntervals.FifteenMinutes)).ToUnixTimeSeconds();
            if (logRequestForm?.Model != null)
            {
                logRequest.Filter = logRequestForm.Model.Filter;
                logRequest.QueryErrors = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Errors);
                logRequest.QueryWarnings = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Warnings);
                logRequest.QueryTraces = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Traces);
                logRequest.QueryEvents = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Events);
                logRequest.QueryMetrics = logRequestForm.Model.QueryTypes.Contains(LogQueryTypes.Metrics);
            }
            else
            {
                logRequest.QueryErrors = true;
                logRequest.QueryWarnings = true;
                if (!IsMasterTenant && !IsMasterTrack)
                {
                    logRequest.QueryEvents = true;
                }
            }
        }

        private DateTimeOffset? GetFromTime()
        {            
            if (!string.IsNullOrWhiteSpace(logRequestForm?.Model?.FromTime))
            {
                try
                {
                    return DateTimeOffset.Parse(logRequestForm.Model.FromTime);
                }
                catch (Exception tex)
                {
                    logRequestForm.SetFieldError(nameof(logRequestForm.Model.FromTime), tex.Message);
                    return null;
                }
            }

            return LogRequestViewModel.DefaultFromTime;
        }

        private void OnClientSettingLoaded()
        {
            StateHasChanged();
        }

        private void LogRequestViewModelAfterInit(LogRequestViewModel model)
        {
            if (!(model.QueryTypes?.Count() > 0))
            {
                model.QueryTypes = [LogQueryTypes.Errors, LogQueryTypes.Warnings];
                if (!IsMasterTenant && !IsMasterTrack)
                {
                    model.QueryTypes.Add(LogQueryTypes.Events);
                }
            }

            if (ClientSettings.LogOption == LogOptions.ApplicationInsights)
            {
                model.DisableBothEventAndTrace = true;
            }
        }

        private async Task OnLogRequestValidSubmitAsync(EditContext editContext)
        {
            if (logRequestForm.Model.QueryTypes.Count() <= 0)
            {
                logRequestForm.Model.QueryTypes.Add(LogQueryTypes.Errors);
                logRequestForm.Model.QueryTypes.Add(LogQueryTypes.Warnings);
                if (!IsMasterTenant && !IsMasterTrack)
                {
                    logRequestForm.Model.QueryTypes.Add(LogQueryTypes.Events);
                }
            }

            if (IsMasterTenant && logRequestForm.Model.TenantName.IsNullOrWhiteSpace())
            {
                logRequestForm.Model.TrackName = null;
            }

            logResponse = null;
            await LoadLogAsync();
        }
    }
}
