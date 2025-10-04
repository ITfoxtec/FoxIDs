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
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace FoxIDs.Client.Pages.Logging
{
    public partial class LogAudit
    {
        private static readonly IReadOnlyList<AuditType> auditTypes = Enum.GetValues<AuditType>();
        private string auditLoadError;
        private PageEditForm<AuditLogRequestViewModel> auditLogRequestForm;
        private LogResponseViewModel auditLogResponse;
        private string logsHref;
        private string logUsageHref;
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
            logsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logs";
            logUsageHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logusage";
            logSettingsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logsettings";
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
                await LoadAuditLogAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                auditLogResponse = null;
                auditLoadError = ex.Message;
            }
        }

        private async Task LoadAuditLogAsync()
        {
            auditLoadError = null;

            if (IsMasterTenant)
            {
                var auditLogRequest = new TenantAuditLogRequest();
                if (!AddAuditLogValues(auditLogRequest))
                {
                    return;
                }
                if (auditLogRequestForm?.Model != null)
                {
                    auditLogRequest.TenantName = auditLogRequestForm.Model.TenantName?.Trim();
                    auditLogRequest.TrackName = auditLogRequestForm.Model.TrackName?.Trim();
                }
                auditLogResponse = (await TenantService.GetAuditLogAsync(auditLogRequest)).Map<LogResponseViewModel>();
            }
            else if (IsMasterTrack)
            {
                var auditLogRequest = new TenantAuditLogRequest();
                if (!AddAuditLogValues(auditLogRequest))
                {
                    return;
                }
                if (auditLogRequestForm?.Model != null)
                {
                    auditLogRequest.TrackName = auditLogRequestForm.Model.TrackName?.Trim();
                }
                auditLogResponse = (await MyTenantService.GetAuditLogAsync(auditLogRequest)).Map<LogResponseViewModel>();
            }
            else
            {
                var auditLogRequest = new AuditLogRequest();
                if (!AddAuditLogValues(auditLogRequest))
                {
                    return;
                }
                auditLogResponse = (await TrackService.GetTrackAuditLogAsync(auditLogRequest)).Map<LogResponseViewModel>();
            }
        }

        private bool AddAuditLogValues(AuditLogRequest auditLogRequest)
        {
            var fromTime = GetFromTime();
            if (!fromTime.HasValue)
            {
                return false;
            }

            auditLogRequest.FromTime = fromTime.Value.ToUnixTimeSeconds();
            var interval = (int)(auditLogRequestForm?.Model?.TimeInterval ?? LogTimeIntervals.FifteenMinutes);
            auditLogRequest.ToTime = fromTime.Value.AddSeconds(interval).ToUnixTimeSeconds();

            if (auditLogRequestForm?.Model != null)
            {
                var model = auditLogRequestForm.Model;
                auditLogRequest.Filter = model.Filter?.Trim();
                auditLogRequest.AuditType = model.AuditType;
            }

            return true;
        }

        private DateTimeOffset? GetFromTime()
        {
            if (!string.IsNullOrWhiteSpace(auditLogRequestForm?.Model?.FromTime))
            {
                try
                {
                    return DateTimeOffset.Parse(auditLogRequestForm.Model.FromTime);
                }
                catch (Exception tex)
                {
                    auditLogRequestForm.SetFieldError(nameof(auditLogRequestForm.Model.FromTime), tex.Message);
                    return null;
                }
            }

            return LogRequestViewModel.DefaultFromTime;
        }

        private void OnClientSettingLoaded()
        {
            StateHasChanged();
        }

        private async Task OnAuditLogRequestValidSubmitAsync(EditContext editContext)
        {
            auditLogRequestForm.Model.TenantName = auditLogRequestForm.Model.TenantName?.Trim();
            auditLogRequestForm.Model.TrackName = auditLogRequestForm.Model.TrackName?.Trim();

            if (IsMasterTenant && auditLogRequestForm.Model.TenantName.IsNullOrWhiteSpace())
            {
                auditLogRequestForm.Model.TrackName = null;
            }

            auditLogResponse = null;
            await LoadAuditLogAsync();
        }

        private RenderFragment RenderAuditMetadata(LogItemViewModel item) => builder =>
        {
            if (item?.Values == null)
            {
                return;
            }

            item.Values.TryGetValue(Constants.Logs.AuditDataType, out var dataType);
            item.Values.TryGetValue(Constants.Logs.AuditDataAction, out var dataAction);
            item.Values.TryGetValue(Constants.Logs.DocumentId, out var documentId);

            var seq = 0;
            builder.OpenElement(seq++, "div");

            if (!dataType.IsNullOrWhiteSpace())
            {
                builder.OpenElement(seq++, "div");
                builder.AddContent(seq++, $"Data type: {dataType}");
                builder.CloseElement();
            }

            if (!dataAction.IsNullOrWhiteSpace())
            {
                builder.OpenElement(seq++, "div");
                builder.AddContent(seq++, $"Action: {dataAction}");
                builder.CloseElement();
            }

            if (!documentId.IsNullOrWhiteSpace())
            {
                builder.OpenElement(seq++, "div");
                builder.AddContent(seq++, $"DocumentId: {documentId}");
                builder.CloseElement();
            }

            builder.CloseElement();
        };

        private RenderFragment RenderData(LogItemViewModel item) => builder =>
        {
            if (!TryGetAuditData(item, out var data))
            {
                return;
            }

            var seq = 0;
            builder.OpenElement(seq++, "pre");
            builder.AddAttribute(seq++, "class", "mt-2 mb-0");
            builder.AddAttribute(seq++, "style", "max-height:20rem; overflow:auto;");
            builder.OpenElement(seq++, "code");
            builder.AddContent(seq++, data);
            builder.CloseElement();
            builder.CloseElement();
        };

        private bool TryGetAuditData(LogItemViewModel item, out string formattedJson)
        {
            formattedJson = null;
            if (item?.Values == null)
            {
                return false;
            }

            if (!item.Values.TryGetValue(Constants.Logs.Data, out var data) || data.IsNullOrWhiteSpace())
            {
                return false;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(data);
                formattedJson = JsonSerializer.Serialize(jsonDoc.RootElement, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            }
            catch (JsonException)
            {
                formattedJson = data;
            }

            return true;
        }
    }
}
