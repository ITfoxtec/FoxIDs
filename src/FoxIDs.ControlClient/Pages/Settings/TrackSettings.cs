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
using FoxIDs.Client.Models.Config;

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
        private PageEditForm<TrackSettingsViewModel> trackSettingsForm;
        private string deleteTrackError;
        private bool deleteTrackAcknowledge = false;
        private string deleteTrackAcknowledgeText = string.Empty;
        private bool trackWorking;

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        private bool IsMasterTrack => RouteBindingLogic.IsMasterTrack;

        protected override async Task OnInitializedAsync()
        {
            mailSettingsHref = $"{TenantName}/mailsettings";
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
        }

        private async Task DefaultLoadAsync()
        {
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
                await TrackService.UpdateTrackAsync(trackSettingsForm.Model.Map<Track>());
                toastService.ShowSuccess("Track settings updated.");
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
                deleteTrackError = "Please type 'delete' to confirm that you want to delete.";
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
                deleteTrackError = ex.Message;
            }
        }
    }
}
