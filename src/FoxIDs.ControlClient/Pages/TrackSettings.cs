using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using Blazored.Toast.Services;

namespace FoxIDs.Client.Pages
{
    public partial class TrackSettings
    {
        private string tenantSettingsHref;
        private string mailSettingsHref;
        private string claimMappingsHref;
        private PageEditForm<TrackSettingsViewModel> trackSettingsForm;
        private string deleteTrackError;
        private bool deleteTrackAcknowledge = false;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        private bool IsMasterTrack => trackSettingsForm != null && Constants.Routes.MasterTrackName.Equals(trackSettingsForm.Model.Name, StringComparison.OrdinalIgnoreCase);

        protected override async Task OnInitializedAsync()
        {
            tenantSettingsHref = $"{TenantName}/tenantsettings";
            mailSettingsHref = $"{TenantName}/mailsettings";
            claimMappingsHref = $"{TenantName}/claimmappings";
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

        private void UpdateTrackViewModelAfterInit(TrackSettingsViewModel updateTrackViewModel)
        {
            updateTrackViewModel.FormattedName = updateTrackViewModel.Name.FormatTrackName();
        }

        private async Task OnUpdateTrackValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TrackService.UpdateTrackAsync(trackSettingsForm.Model.Map<Track>());
                toastService.ShowSuccess("Track settings updated.");
            }
            catch (Exception ex)
            {
                trackSettingsForm.SetError(ex.Message);
            }
        }

        private async Task DeleteTrackAsync()
        {
            try
            {
                await TrackService.DeleteTrackAsync(TrackSelectedLogic.Track.Name);
                await TrackSelectedLogic.ShowSelectTrackAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                deleteTrackError = ex.Message;
            }
        }
    }
}
