using Blazored.Toast.Services;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using FoxIDs.Util;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages.Settings
{
    public partial class TrackSettings
    {
        private string mailSettingsHref;
        private string claimMappingsHref;
        private string textsHref;
        private string plansHref;
        private string smsPricesHref;
        private string riskPasswordsHref;
        private string smsSettingsHref;
        private PageEditForm<TrackSettingsViewModel> trackSettingsForm;
        private string deleteTrackError;
        private bool deleteTrackAcknowledge = false;
        private string deleteTrackAcknowledgeText = string.Empty;
        private bool trackWorking;
        private bool scrollToTopAfterLoad;

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        private bool IsMasterTrack => RouteBindingLogic.IsMasterTrack;

        protected override async Task OnInitializedAsync()
        {
            mailSettingsHref = $"{TenantName}/mailsettings";
            smsSettingsHref = $"{TenantName}/smssettings";
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
            if (scrollToTopAfterLoad)
            {
                scrollToTopAfterLoad = false;
                await ScrollToTopAsync();
            }
        }

        private async Task DefaultLoadAsync()
        {
            trackSettingsForm?.ClearError();
            try
            {
                trackWorking = false;
                deleteTrackError = null;
                deleteTrackAcknowledge = false;
                var track = await TrackService.GetTrackAsync(TrackSelectedLogic.Track.Name);
                await trackSettingsForm.InitAsync(track.Map<TrackSettingsViewModel>());
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                trackSettingsForm.SetError(ex.Message);
            }
        }

        private async Task OnUpdateTrackValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if (trackWorking)
                {
                    return;
                }
                trackWorking = true;
                var trackSettingsResult = await TrackService.UpdateTrackAsync(trackSettingsForm.Model.Map<Track>(afterMap: afterMap =>
                {
                    if (string.IsNullOrWhiteSpace(afterMap.ExternalPassword?.ApiUrl))
                    {
                        afterMap.ExternalPassword = null;
                    }
                }));
                trackSettingsForm.UpdateModel(trackSettingsResult.Map<TrackSettingsViewModel>());
                TrackSelectedLogic.UpdateTrack(trackSettingsResult);
                toastService.ShowSuccess("Track settings have been updated.");
                trackWorking = false;
            }
            catch (Exception ex)
            {
                trackWorking = false;
                trackSettingsForm.SetError(ex.Message);
            }
        }

        private async Task DeleteTrackAsync()
        {
            deleteTrackError = string.Empty;
            if (!"delete".Equals(deleteTrackAcknowledgeText, StringComparison.InvariantCultureIgnoreCase))
            {
                deleteTrackError = "Type 'delete' to confirm track deletion.";
                return;
            }

            try
            {
                deleteTrackAcknowledge = false;
                deleteTrackAcknowledgeText = string.Empty;
                if (trackWorking)
                {
                    return;
                }
                trackWorking = true;
                await TrackService.DeleteTrackAsync(TrackSelectedLogic.Track.Name);
                scrollToTopAfterLoad = true;
                await TrackSelectedLogic.SelectTrackAsync();
                trackWorking = false;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                trackWorking = false;
                scrollToTopAfterLoad = false;
                deleteTrackError = ex.Message;
            }
        }

        private async Task ScrollToTopAsync()
        {
            if (JSRuntime == null)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("window.scrollTo", 0d, 0d);
            }
            catch (JSException)
            {
                // Ignore scroll errors to avoid blocking UI updates.
            }
        }

        private void AddPasswordPolicy()
        {
            if (trackSettingsForm.Model.PasswordPolicies.Count >= Constants.Models.Track.PasswordPoliciesMax)
            {
                toastService.ShowError($"Maximum number of password policies reached ({Constants.Models.Track.PasswordPoliciesMax}).");
                return;
            }

            trackSettingsForm.Model.PasswordPolicies.Add(new PasswordPolicyViewModel
            {
                Name = RandomName.GenerateDefaultName(),
                Length = trackSettingsForm.Model.PasswordLength,
                MaxLength = trackSettingsForm.Model.PasswordMaxLength,
                CheckComplexity = trackSettingsForm.Model.CheckPasswordComplexity ?? true,
                CheckRisk = trackSettingsForm.Model.CheckPasswordRisk ?? true,
                History = trackSettingsForm.Model.PasswordHistory,
                MaxAge = trackSettingsForm.Model.PasswordMaxAge,
                SoftChange = trackSettingsForm.Model.SoftPasswordChange,
                BannedCharacters = trackSettingsForm.Model.PasswordBannedCharacters
            });
        }

        private void RemovePasswordPolicy(List<PasswordPolicyViewModel> passwordPolicies, PasswordPolicyViewModel policy)
        {
            passwordPolicies.Remove(policy);
        }
    }
}
