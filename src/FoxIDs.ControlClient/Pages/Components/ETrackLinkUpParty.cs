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
using ITfoxtec.Identity;
using System.Net.Http;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ETrackLinkUpParty : UpPartyBase
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (!UpParty.CreateMode)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                var generalTrackLinkUpParty = UpParty as GeneralTrackLinkUpPartyViewModel;
                var trackLinkUpParty = await UpPartyService.GetTrackLinkUpPartyAsync(UpParty.Name);
                await generalTrackLinkUpParty.Form.InitAsync(ToViewModel(trackLinkUpParty));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                UpParty.Error = ex.Message;
            }
        }

        private TrackLinkUpPartyViewModel ToViewModel(TrackLinkUpParty trackLinkUpParty)
        {
            return trackLinkUpParty.Map<TrackLinkUpPartyViewModel>(afterMap =>
            {
                afterMap.EnableSingleLogout = !trackLinkUpParty.DisableSingleLogout;
                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void TrackLinkUpPartyViewModelAfterInit(GeneralTrackLinkUpPartyViewModel trackLinkUpParty, TrackLinkUpPartyViewModel model)
        {
            if (trackLinkUpParty.CreateMode)
            {
                model.SelectedUpParties = new List<string> { "*" };
                model.Claims = new List<string> { "*" };
            }
        }

        private async Task OnEditTrackLinkUpPartyValidSubmitAsync(GeneralTrackLinkUpPartyViewModel generalTrackLinkUpParty, EditContext editContext)
        {
            try
            {
                if(generalTrackLinkUpParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalTrackLinkUpParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var trackLinkUpParty = generalTrackLinkUpParty.Form.Model.Map<TrackLinkUpParty>(afterMap: afterMap =>
                {
                    afterMap.DisableSingleLogout = !generalTrackLinkUpParty.Form.Model.EnableSingleLogout;
                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                });

                if (generalTrackLinkUpParty.CreateMode)
                {
                    var trackLinkUpPartyResult = await UpPartyService.CreateTrackLinkUpPartyAsync(trackLinkUpParty);
                    generalTrackLinkUpParty.Form.UpdateModel(ToViewModel(trackLinkUpPartyResult));
                    generalTrackLinkUpParty.CreateMode = false;
                    toastService.ShowSuccess("OpenID Connect Up-party created.");
                }
                else
                {
                    var trackLinkUpPartyResult = await UpPartyService.UpdateTrackLinkUpPartyAsync(trackLinkUpParty);
                    generalTrackLinkUpParty.Form.UpdateModel(ToViewModel(trackLinkUpPartyResult));
                    toastService.ShowSuccess("OpenID Connect Up-party updated.");
                }

                generalTrackLinkUpParty.Name = generalTrackLinkUpParty.Form.Model.Name;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalTrackLinkUpParty.Form.SetFieldError(nameof(generalTrackLinkUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteTrackLinkUpPartyAsync(GeneralTrackLinkUpPartyViewModel generalTrackLinkUpParty)
        {
            try
            {
                await UpPartyService.DeleteTrackLinkUpPartyAsync(generalTrackLinkUpParty.Name);
                UpParties.Remove(generalTrackLinkUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalTrackLinkUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
