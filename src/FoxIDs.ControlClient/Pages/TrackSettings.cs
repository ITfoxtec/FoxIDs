using FoxIDs.Client.Infrastructure;
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
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages
{
    public partial class TrackSettings
    {
        private string claimMappingsHref;
        private PageEditForm<UpdateTrackViewModel> updateTrackForm;
        private string deleteTrackError;
        private bool deleteTrackAcknowledge = false;

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
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
                var trackSendEmail = await TrackService.GetTrackSendEmailAsync();           
                await updateTrackForm.InitAsync(track.Map<UpdateTrackViewModel>(afterMap: afterMap => 
                {
                    if (trackSendEmail != null)
                    {
                        afterMap.SendMailExist = true;
                        afterMap.FromEmail = trackSendEmail.FromEmail;
                        afterMap.SendgridApiKey = trackSendEmail.SendgridApiKey;
                    }
                }));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                updateTrackForm.SetError(ex.Message);
            }
        }


        private void UpdateTrackViewModelAfterInit(UpdateTrackViewModel updateTrackViewModel)
        {
            updateTrackViewModel.Name = updateTrackViewModel.Name.FormatTrackName();
        }

        private async Task OnUpdateTrackValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TrackService.UpdateTrackAsync(updateTrackForm.Model.Map<Track>());
                if (!updateTrackForm.Model.FromEmail.IsNullOrWhiteSpace() && !updateTrackForm.Model.SendgridApiKey.IsNullOrWhiteSpace())
                {
                    await TrackService.UpdateTrackSendEmailAsync(new SendEmail
                    {
                        FromEmail = updateTrackForm.Model.FromEmail,
                        SendgridApiKey = updateTrackForm.Model.SendgridApiKey
                    });
                }
                else if (updateTrackForm.Model.SendMailExist)
                {
                    await TrackService.DeleteTrackSendEmailAsync();
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    updateTrackForm.SetFieldError(nameof(updateTrackForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
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
