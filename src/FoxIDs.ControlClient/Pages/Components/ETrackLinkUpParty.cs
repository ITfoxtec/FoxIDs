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
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapOAuthClaimTransforms();
                }

                afterMap.ExtendedUis.MapExtendedUis();

                if (afterMap.ExitClaimTransforms?.Count > 0)
                {
                    afterMap.ExitClaimTransforms = afterMap.ExitClaimTransforms.MapOAuthClaimTransforms();
                }
                if (afterMap.LinkExternalUser?.ClaimTransforms?.Count > 0)
                {
                    afterMap.LinkExternalUser.ClaimTransforms = afterMap.LinkExternalUser.ClaimTransforms.MapOAuthClaimTransforms();
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
                generalTrackLinkUpParty.Form.Model.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalTrackLinkUpParty.Form.Model.ExtendedUis.MapExtendedUisBeforeMap();
                generalTrackLinkUpParty.Form.Model.ExitClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalTrackLinkUpParty.Form.Model.LinkExternalUser?.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();

                var trackLinkUpParty = generalTrackLinkUpParty.Form.Model.Map<TrackLinkUpParty>(afterMap: afterMap =>
                {
                    afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    afterMap.ExtendedUis.MapExtendedUisAfterMap();
                    afterMap.ExitClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    afterMap.LinkExternalUser = afterMap.LinkExternalUser.MapLinkExternalUserAfterMap();
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
