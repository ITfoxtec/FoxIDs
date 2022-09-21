using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class LogUsages
    {
        private List<string> includeTypeItems = new List<string> { UsageLogIncludeTypes.Logins, UsageLogIncludeTypes.TokenRequests, UsageLogIncludeTypes.ControlApiGets, UsageLogIncludeTypes.ControlApiUpdates };

        private string logLoadError;
        private PageEditForm<UsageLogRequestViewModel> usageLogRequestForm;
        private UsageLogResponse usageLogResponse;
        private string logsHref;
        private string logSettingsHref;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            logsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logs";
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
                await LoadUsageLogAsync();
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

        private async Task LoadUsageLogAsync()
        {
            logLoadError = null;
            var usageLogRequest = new UsageLogRequest();
            if (usageLogRequestForm?.Model != null)
            {
                usageLogRequest.TimeScope = usageLogRequestForm.Model.TimeScope;
                usageLogRequest.SummarizeLevel = usageLogRequestForm.Model.SummarizeLevel;
                usageLogRequest.IncludeLogins = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.Logins);
                usageLogRequest.IncludeTokenRequests = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.TokenRequests);
                usageLogRequest.IncludeControlApiGets = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.ControlApiGets);
                usageLogRequest.IncludeControlApiUpdates = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.ControlApiUpdates);
            }
            else 
            {
                usageLogRequest.TimeScope = UsageLogTimeScopes.ThisMonth;
                usageLogRequest.SummarizeLevel = UsageLogSummarizeLevels.Month;
                usageLogRequest.IncludeLogins = true;
                usageLogRequest.IncludeTokenRequests = true;
            }

            usageLogResponse = await TrackService.GetTrackUsageLogAsync(usageLogRequest);
        }

        private async Task OnUsageLogRequestValidSubmitAsync()
        {
            if (usageLogRequestForm.Model.IncludeTypes.Count() <= 0)
            {
                usageLogRequestForm.Model.IncludeTypes.Add(UsageLogIncludeTypes.Logins);
                usageLogRequestForm.Model.IncludeTypes.Add(UsageLogIncludeTypes.TokenRequests);
            }

            usageLogResponse = null;
            await LoadUsageLogAsync();
        }
    }
}
