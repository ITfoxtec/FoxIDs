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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using FoxIDs.Util;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ETrackLinkUpParty : UpPartyBase
    {
        [Inject]
        public TrackService TrackService { get; set; }

        [Inject]
        public DownPartyService DownPartyService { get; set; }


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
                if (afterMap.Profiles != null)
                {
                    foreach (var profile in afterMap.Profiles)
                    {
                        profile.InitName = profile.Name;
                    }
                }

                if (afterMap.DisplayName.IsNullOrWhiteSpace())
                {
                    afterMap.DisplayName = afterMap.Name;
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
                if (afterMap.LinkExternalUser?.ClaimTransforms?.Count > 0)
                {
                    afterMap.LinkExternalUser.ClaimTransforms = afterMap.LinkExternalUser.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private async Task TrackLinkUpPartyViewModelAfterInitAsync(TrackLinkUpPartyViewModel model)
        {
            try
            {
                var track = await TrackService.GetTrackAsync(model.ToDownTrackName);
                model.ToDownTrackDisplayName = track.DisplayName;

                var downParty = await DownPartyService.GetTrackLinkDownPartyAsync(model.ToDownPartyName, trackName: model.ToDownTrackName);
                model.ToDownPartyDisplayName = downParty.DisplayName;
            }
            catch { }
        }

        private void AddProfile(MouseEventArgs e, List<TrackLinkUpPartyProfileViewModel> profiles)
        {
            var profile = new TrackLinkUpPartyProfileViewModel
            {
                Name = RandomName.GenerateDefaultName(profiles.Select(p => p.Name))
            };
            profiles.Add(profile);
        }

        private void RemoveProfile(MouseEventArgs e, List<TrackLinkUpPartyProfileViewModel> profiles, TrackLinkUpPartyProfileViewModel removeProfile)
        {
            profiles.Remove(removeProfile);
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
                if (generalTrackLinkUpParty.Form.Model.LinkExternalUser?.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalTrackLinkUpParty.Form.Model.LinkExternalUser.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var trackLinkUpParty = generalTrackLinkUpParty.Form.Model.Map<TrackLinkUpParty>(afterMap: afterMap =>
                {
                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(afterMap.LinkExternalUser?.LinkClaimType) && !(afterMap.LinkExternalUser?.AutoCreateUser == true || afterMap.LinkExternalUser?.RequireUser == true))
                    {
                        afterMap.LinkExternalUser = null;
                    }
                    if (afterMap.LinkExternalUser != null)
                    {
                        if (afterMap.LinkExternalUser.Elements?.Count() > 0)
                        {
                            int order = 1;
                            foreach (var element in afterMap.LinkExternalUser.Elements)
                            {
                                element.Order = order++;
                            }
                        }
                        if (afterMap.LinkExternalUser.ClaimTransforms?.Count() > 0)
                        {
                            int order = 1;
                            foreach (var claimTransform in afterMap.LinkExternalUser.ClaimTransforms)
                            {
                                claimTransform.Order = order++;
                            }
                        }
                    }
                });

                if (!generalTrackLinkUpParty.CreateMode)
                {
                    if (generalTrackLinkUpParty.Form.Model.Profiles?.Count() > 0)
                    {
                        foreach (var profile in generalTrackLinkUpParty.Form.Model.Profiles)
                        {
                            if (!profile.InitName.IsNullOrWhiteSpace() && profile.InitName != profile.Name)
                            {
                                var profileMap = trackLinkUpParty.Profiles?.Where(p => p.Name == profile.Name).First();
                                profileMap.Name = profile.InitName;
                                profileMap.NewName = profile.Name;
                            }
                        }
                    }

                    var trackLinkUpPartyResult = await UpPartyService.UpdateTrackLinkUpPartyAsync(trackLinkUpParty);
                    generalTrackLinkUpParty.Form.UpdateModel(ToViewModel(trackLinkUpPartyResult));
                    toastService.ShowSuccess("Environment Link authentication method updated.");
                    generalTrackLinkUpParty.DisplayName = trackLinkUpPartyResult.DisplayName;
                    generalTrackLinkUpParty.Profiles = trackLinkUpPartyResult.Profiles?.Map<List<UpPartyProfile>>();
                }
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
                try
                {
                    await DownPartyService.DeleteTrackLinkDownPartyAsync(generalTrackLinkUpParty.Form.Model.ToDownPartyName, trackName: generalTrackLinkUpParty.Form.Model.ToDownTrackName);
                }
                catch
                { }
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
