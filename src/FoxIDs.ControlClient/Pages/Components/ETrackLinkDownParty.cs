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

namespace FoxIDs.Client.Pages.Components
{
    public partial class ETrackLinkDownParty : DownPartyBase
    {
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (!DownParty.CreateMode)
            {
                await DefaultLoadAsync();
            }
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
                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void TrackLinkDownPartyViewModelAfterInit(GeneralTrackLinkDownPartyViewModel trackLinkDownParty, TrackLinkDownPartyViewModel model)
        {
            if (trackLinkDownParty.CreateMode)
            {
                model.Claims = new List<OAuthDownClaim>
                {
                    new OAuthDownClaim { Claim = "*" }
                };
            }
        }

        private void AddTrackLinkClaim(MouseEventArgs e, List<OAuthDownClaim> claims)
        {
            claims.Add(new OAuthDownClaim());
        }

        private void RemoveTrackLinkClaim(MouseEventArgs e, List<OAuthDownClaim> claims, OAuthDownClaim removeClaim)
        {
            claims.Remove(removeClaim);
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

                TrackLinkDownParty trackLinkDownPartyResult;
                if (generalTrackLinkDownParty.CreateMode)
                {
                    trackLinkDownPartyResult = await DownPartyService.CreateTrackLinkDownPartyAsync(trackLinkDownParty);
                }
                else
                {
                    trackLinkDownPartyResult = await DownPartyService.UpdateTrackLinkDownPartyAsync(trackLinkDownParty);
                }

                generalTrackLinkDownParty.Form.UpdateModel(ToViewModel(trackLinkDownPartyResult));
                if (generalTrackLinkDownParty.CreateMode)
                {
                    generalTrackLinkDownParty.CreateMode = false;
                    toastService.ShowSuccess("Track link down-party created.");
                }
                else
                {
                    toastService.ShowSuccess("Track link down-party updated.");
                }
                generalTrackLinkDownParty.Name = generalTrackLinkDownParty.Form.Model.Name;
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
