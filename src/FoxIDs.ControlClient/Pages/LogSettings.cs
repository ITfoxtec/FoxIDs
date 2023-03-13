using Blazored.Toast.Services;
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
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class LogSettings
    {
        private string logsHref;
        private string logUsagesHref;
        private GeneralLogSettingsViewModel generalLogSettings = new GeneralLogSettingsViewModel();
        private string logSreamSettingsListError;
        private List<GeneralLogStreamSettingsViewModel> logSreamSettingsList = new List<GeneralLogStreamSettingsViewModel>();

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
            logsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logs";
            logUsagesHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logusages";
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
            logSreamSettingsListError = null;
            try
            {
                var logStreams = await TrackService.GetTrackLogStreamSettingsAsync();
                logSreamSettingsList.Clear();
                if (logStreams?.LogStreamSettings?.Count > 0)
                {
                    foreach (var ls in logStreams?.LogStreamSettings)
                    {
                        logSreamSettingsList.Add(new GeneralLogStreamSettingsViewModel(ls));
                    }
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                logSreamSettingsListError = ex.Message;
            }
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
                toastService.ShowSuccess("Log settings updated.");
            }
            catch (Exception ex)
            {
                generalLogSettings.Form.SetError(ex.Message);
            }
        }

        private string LogStreamSettingsInfoText(GeneralLogStreamSettingsViewModel generalLogStreamSettings)
        {
            if (generalLogStreamSettings.LogStreamSettings.Type == LogStreamTypes.ApplicationInsights)
            {
                var connectionStringSplit = generalLogStreamSettings.LogStreamSettings.ApplicationInsightsSettings.ConnectionString?.Split(';');
                return $"Application Insights {connectionStringSplit?.FirstOrDefault()}";
            }
            else
            {
                throw new NotSupportedException("Log stream settings type not supported.");
            }
        }

        private void ShowCreateLogStreamApplicationInsights()
        {
            var logStreamSettings = new GeneralLogStreamSettingsViewModel();
            logStreamSettings.CreateMode = true;
            logStreamSettings.Edit = true;
            logSreamSettingsList.Add(logStreamSettings);
        }

        private void ShowUpdateLogStreamSettings(GeneralLogStreamSettingsViewModel generalLogStreamSettings)
        {
            generalLogStreamSettings.CreateMode = false;
            generalLogStreamSettings.DeleteAcknowledge = false;
            logSreamSettingsListError = null;
            generalLogStreamSettings.Error = null;
            generalLogStreamSettings.Edit = true;
        }

        private async Task LogStreamSettingsViewModelAfterInitializedAsync(PageEditForm<LogStreamSettings> form, GeneralLogStreamSettingsViewModel generalLogStreamSettings)
        {
            try
            {
                await form.InitAsync(generalLogStreamSettings.LogStreamSettings);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalLogStreamSettings.Error = ex.Message;
            }
        }

        private void LogStreamSettingsViewModelAfterInit(GeneralLogStreamSettingsViewModel generalLogStreamSettings, LogStreamSettings LogStreamSettings)
        {
            if (LogStreamSettings.Type == LogStreamTypes.ApplicationInsights)
            {
                if (LogStreamSettings.ApplicationInsightsSettings == null)
                {
                    LogStreamSettings.ApplicationInsightsSettings = new LogStreamApplicationInsightsSettings();
                }
            }
            else
            {
                throw new NotSupportedException("Log stream settings type not supported.");
            }

            if (generalLogStreamSettings.CreateMode)
            {
                LogStreamSettings.LogWarning = true;
                LogStreamSettings.LogError = true;
                LogStreamSettings.LogCriticalError = true;
                LogStreamSettings.LogEvent = true;
            }
        }

        private void LogStreamSettingsCancel(GeneralLogStreamSettingsViewModel generalLogStreamSettings)
        {
            if (generalLogStreamSettings.CreateMode)
            {
                logSreamSettingsList.Remove(generalLogStreamSettings);
            }
            else
            {
                generalLogStreamSettings.Edit = false;
            }
        }

        private async Task OnEditLogStreamSettingsValidSubmitAsync(GeneralLogStreamSettingsViewModel generalLogStreamSettings, EditContext editContext)
        {
            var logStreams = new LogStreams { LogStreamSettings = new List<LogStreamSettings>() };
            GeneralLogStreamSettingsViewModel updatedgeneralLogStreamSettings = null;
            foreach (var ls in logSreamSettingsList)
            {
                if (ls == generalLogStreamSettings)
                {
                    logStreams.LogStreamSettings.Add(generalLogStreamSettings.Form.Model);
                    updatedgeneralLogStreamSettings = ls;
                }
                else
                {
                    logStreams.LogStreamSettings.Add(ls.LogStreamSettings);
                }
            }
            await TrackService.SaveTrackLogStreamSettingsAsync(logStreams);
            updatedgeneralLogStreamSettings.LogStreamSettings = generalLogStreamSettings.Form.Model;
            if (generalLogStreamSettings.CreateMode)
            {
                toastService.ShowSuccess("Log stream settings created.");
            }
            else
            {
                toastService.ShowSuccess("Log stream settings updated.");
            }
            generalLogStreamSettings.Edit = false;
        }

        private async Task DeleteLogStreamSettingsAsync(GeneralLogStreamSettingsViewModel generalLogStreamSettings)
        {
            try
            {
                var logStreams = new LogStreams { LogStreamSettings = new List<LogStreamSettings>() };
                var logStreamSettingsSavelist = new List<LogStreamSettings>();
                foreach (var ls in logSreamSettingsList)
                {
                    if (ls != generalLogStreamSettings)
                    {
                        logStreams.LogStreamSettings.Add(ls.LogStreamSettings);
                    }
                }
                await TrackService.SaveTrackLogStreamSettingsAsync(logStreams);
                logSreamSettingsList.Remove(generalLogStreamSettings);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalLogStreamSettings.Form.SetError(ex.Message);
            }
        }
    }
}
