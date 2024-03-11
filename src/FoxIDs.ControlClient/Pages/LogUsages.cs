using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
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
        private static List<string> includeNotUserTypeItems = new List<string> { UsageLogIncludeTypes.Logins, UsageLogIncludeTypes.TokenRequests, UsageLogIncludeTypes.ControlApiGets, UsageLogIncludeTypes.ControlApiUpdates };
        private static List<string> includeMasterTenantAllTypeItems = new List<string> { UsageLogIncludeTypes.Tenants, UsageLogIncludeTypes.Tracks, UsageLogIncludeTypes.KeyVaultManagedCertificate, UsageLogIncludeTypes.Users, UsageLogIncludeTypes.Logins, UsageLogIncludeTypes.TokenRequests, UsageLogIncludeTypes.ControlApiGets, UsageLogIncludeTypes.ControlApiUpdates };
        private static List<string> includeMasterTrackAllTypeItems = new List<string> { UsageLogIncludeTypes.Tracks, UsageLogIncludeTypes.KeyVaultManagedCertificate, UsageLogIncludeTypes.Users, UsageLogIncludeTypes.Logins, UsageLogIncludeTypes.TokenRequests, UsageLogIncludeTypes.ControlApiGets, UsageLogIncludeTypes.ControlApiUpdates };
        private static List<string> includeDefaultAllTypeItems = new List<string> { UsageLogIncludeTypes.Users, UsageLogIncludeTypes.Logins, UsageLogIncludeTypes.TokenRequests, UsageLogIncludeTypes.ControlApiGets, UsageLogIncludeTypes.ControlApiUpdates };

        private string logLoadError;
        private PageEditForm<UsageLogRequestViewModel> usageLogRequestForm;
        private UsageLogResponse usageLogResponse;
        private string logsHref;
        private string logSettingsHref;

        public List<string> IncludeTypeItems { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Inject]
        public MyTenantService MyTenantService { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        private bool IsMasterTrack => TrackSelectedLogic.IsTrackSelected && Constants.Routes.MasterTrackName.Equals(TrackSelectedLogic.Track.Name, StringComparison.OrdinalIgnoreCase);


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
            try
            {
                if (IsMasterTenant)
                {
                    IncludeTypeItems = includeMasterTenantAllTypeItems;
                }
                else if (IsMasterTrack)
                {
                    IncludeTypeItems = includeMasterTrackAllTypeItems;
                }
                else
                {
                    IncludeTypeItems = includeDefaultAllTypeItems;
                }
                StateHasChanged();
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
            if (IsMasterTenant)
            {
                var usageLogRequest = new UsageTenantLogRequest();
                AddUsageLogValues(usageLogRequest);
                if (usageLogRequestForm?.Model != null)
                {
                    usageLogRequest.TenantName = usageLogRequestForm.Model.TenantName;
                    usageLogRequest.TrackName = usageLogRequestForm.Model.TrackName;
                }
                usageLogResponse = await TenantService.GetUsageLogAsync(usageLogRequest);
            }
            else if(IsMasterTrack)
            {
                var usageLogRequest = new UsageMyTenantLogRequest();
                AddUsageLogValues(usageLogRequest);
                if (usageLogRequestForm?.Model != null)
                {
                    usageLogRequest.TrackName = usageLogRequestForm.Model.TrackName;
                }
                usageLogResponse = await MyTenantService.GetUsageLogAsync(usageLogRequest);
            }
            else
            {
                var usageLogRequest = new UsageLogRequest();
                AddUsageLogValues(usageLogRequest);
                usageLogResponse = await TrackService.GetTrackUsageLogAsync(usageLogRequest);
            }
        }

        private void AddUsageLogValues(UsageLogRequest usageLogRequest)
        {
            var now = DateTimeOffset.Now;
            usageLogRequest.TimeOffset = now.Offset.Hours;

            if (usageLogRequestForm?.Model != null)
            {
                usageLogRequest.TimeScope = usageLogRequestForm.Model.TimeScope;
                usageLogRequest.SummarizeLevel = usageLogRequestForm.Model.SummarizeLevel;
                if (IsMasterTenant)
                {
                    usageLogRequest.IncludeTenants = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.Tenants);
                    usageLogRequest.IncludeTracks = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.Tracks);
                    usageLogRequest.IncludeKeyVaultManagedCertificates = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.KeyVaultManagedCertificate);
                }
                else if (IsMasterTrack)
                {
                    usageLogRequest.IncludeTracks = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.Tracks);
                    usageLogRequest.IncludeKeyVaultManagedCertificates = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.KeyVaultManagedCertificate);
                }
                usageLogRequest.IncludeUsers = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.Users);
                usageLogRequest.IncludeLogins = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.Logins);
                usageLogRequest.IncludeTokenRequests = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.TokenRequests);
                usageLogRequest.IncludeControlApiGets = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.ControlApiGets);
                usageLogRequest.IncludeControlApiUpdates = usageLogRequestForm.Model.IncludeTypes.Contains(UsageLogIncludeTypes.ControlApiUpdates);
            }
            else
            {
                usageLogRequest.TimeScope = UsageLogTimeScopes.ThisMonth;
                usageLogRequest.SummarizeLevel = UsageLogSummarizeLevels.Month;
                if (IsMasterTenant)
                {
                    usageLogRequest.IncludeTenants = true;
                    usageLogRequest.IncludeTracks = true;
                    usageLogRequest.IncludeKeyVaultManagedCertificates = true;
                }
                else if (IsMasterTrack)
                {
                    usageLogRequest.IncludeTracks = true;
                    usageLogRequest.IncludeKeyVaultManagedCertificates = true;
                }
                usageLogRequest.IncludeUsers = true;
                usageLogRequest.IncludeUsers = true;
                usageLogRequest.IncludeLogins = true;
                usageLogRequest.IncludeTokenRequests = true;
            }
        }

        private async Task OnUsageLogRequestValidSubmitAsync()
        {
            if (usageLogRequestForm.Model.IncludeTypes.Count() <= 0)
            {
                usageLogRequestForm.Model.IncludeTypes.Add(UsageLogIncludeTypes.Logins);
                usageLogRequestForm.Model.IncludeTypes.Add(UsageLogIncludeTypes.TokenRequests);
            }

            if (usageLogRequestForm.Model.TimeScope == UsageLogTimeScopes.ThisMonth && usageLogRequestForm.Model.SummarizeLevel == UsageLogSummarizeLevels.Month)
            {
                if (IsMasterTenant)
                {
                    IncludeTypeItems = includeMasterTenantAllTypeItems;
                }
                else if (IsMasterTrack)
                {
                    IncludeTypeItems = includeMasterTrackAllTypeItems;
                }
                else
                {
                    IncludeTypeItems = includeDefaultAllTypeItems;
                }
            }
            else
            {
                IncludeTypeItems = includeNotUserTypeItems;
            }

            if (IsMasterTenant && usageLogRequestForm.Model.TenantName.IsNullOrWhiteSpace())
            {
                usageLogRequestForm.Model.TrackName = null;
            }

            usageLogResponse = null;
            await LoadUsageLogAsync();
        }
    }
}
