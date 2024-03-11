using FoxIDs.Client.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using Microsoft.AspNetCore.Components.Web;
using ITfoxtec.Identity;
using System.Net.Http;
using Microsoft.AspNetCore.Components;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ETrackLinkDownParty : DownPartyBase
    {
        [Inject]
        public TrackService TrackService { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                var generalTrackLinkDownParty = DownParty as GeneralTrackLinkDownPartyViewModel;
                var trackLinkDownParty = await DownPartyService.GetTrackLinkDownPartyAsync(DownParty.Name);
                await generalTrackLinkDownParty.Form.InitAsync(ToViewModel(trackLinkDownParty));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                DownParty.Error = ex.Message;
            }
        }

        private TrackLinkDownPartyViewModel ToViewModel(TrackLinkDownParty trackLinkDownParty)
        {
            return trackLinkDownParty.Map<TrackLinkDownPartyViewModel>(afterMap =>
            {
                if (afterMap.DisplayName.IsNullOrWhiteSpace())
                {
                    afterMap.DisplayName = afterMap.Name;
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void AddTrackLinkClaim(MouseEventArgs e, List<OAuthDownClaim> claims)
        {
            claims.Add(new OAuthDownClaim());
        }

        private void RemoveTrackLinkClaim(MouseEventArgs e, List<OAuthDownClaim> claims, OAuthDownClaim removeClaim)
        {
            claims.Remove(removeClaim);
        }

        private async Task TrackLinkDownPartyViewModelAfterInitAsync(TrackLinkDownPartyViewModel model)
        {
            try
            {
                var track = await TrackService.GetTrackAsync(model.ToUpTrackName);
                model.ToUpTrackDisplayName = track.DisplayName;

                var upParty = await UpPartyService.GetTrackLinkUpPartyAsync(model.ToUpPartyName, trackName: model.ToUpTrackName);
                model.ToUpPartyDisplayName = upParty.DisplayName;
            }
            catch { }
        }

        private async Task OnEditTrackLinkDownPartyValidSubmitAsync(GeneralTrackLinkDownPartyViewModel generalTrackLinkDownParty, EditContext editContext)
        {
            try
            {
                if(generalTrackLinkDownParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalTrackLinkDownParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var trackLinkDownParty = generalTrackLinkDownParty.Form.Model.Map<TrackLinkDownParty>(afterMap: afterMap =>
                {
                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                });

                var trackLinkDownPartyResult = await DownPartyService.UpdateTrackLinkDownPartyAsync(trackLinkDownParty);

                generalTrackLinkDownParty.Form.UpdateModel(ToViewModel(trackLinkDownPartyResult));
                toastService.ShowSuccess("Environment Link authentication method updated.");
                generalTrackLinkDownParty.DisplayName = trackLinkDownPartyResult.DisplayName;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalTrackLinkDownParty.Form.SetFieldError(nameof(generalTrackLinkDownParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteTrackLinkDownPartyAsync(GeneralTrackLinkDownPartyViewModel generalTrackLinkDownParty)
        {
            try
            {
                await DownPartyService.DeleteTrackLinkDownPartyAsync(generalTrackLinkDownParty.Name);
                try
                {
                    await UpPartyService.DeleteTrackLinkUpPartyAsync(generalTrackLinkDownParty.Form.Model.ToUpPartyName, trackName: generalTrackLinkDownParty.Form.Model.ToUpTrackName);
                }
                catch
                { }
                DownParties.Remove(generalTrackLinkDownParty);
                await OnStateHasChanged.InvokeAsync(DownParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalTrackLinkDownParty.Form.SetError(ex.Message);
            }
        }
    }
}
