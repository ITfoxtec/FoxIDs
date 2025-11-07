using FoxIDs.Client.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using ITfoxtec.Identity;
using System.Net.Http;
using FoxIDs.Util;
using Microsoft.AspNetCore.Components.Web;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EExternalLoginUpParty : UpPartyBase
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
                var generalExtLoginUpParty = UpParty as GeneralExternalLoginUpPartyViewModel;                
                var extLoginUpParty = await UpPartyService.GetExternalLoginUpPartyAsync(UpParty.Name, cancellationToken: ComponentCancellationToken);
                await generalExtLoginUpParty.Form.InitAsync(ToViewModel(extLoginUpParty));
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

        private ExternalLoginUpPartyViewModel ToViewModel(ExternalLoginUpParty extLoginUpParty)
        {
            return extLoginUpParty.Map<ExternalLoginUpPartyViewModel>(afterMap: afterMap =>
            {
                afterMap.InitName = afterMap.Name;
                if (afterMap.Profiles?.Count() > 0)
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

                if (afterMap.Secret != null)
                {
                    afterMap.Secret = afterMap.SecretLoaded = afterMap.Secret.Length == 3 ? $"{afterMap.Secret}..." : afterMap.Secret;
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

        private async Task ExternalLoginUpPartyViewModelAfterInitAsync(GeneralExternalLoginUpPartyViewModel extLoginParty, ExternalLoginUpPartyViewModel model)
        {
            if (extLoginParty.CreateMode)
            {
                model.Name = await UpPartyService.GetNewPartyNameAsync(cancellationToken: ComponentCancellationToken);
            }
        }

        private void AddProfile(MouseEventArgs e, List<ExternalLoginUpPartyProfileViewModel> profiles)
        {
            var profile = new ExternalLoginUpPartyProfileViewModel
            {
                Name = RandomName.GenerateDefaultName(profiles.Select(p => p.Name))
            };
            profiles.Add(profile);
        }

        private void RemoveProfile(MouseEventArgs e, List<ExternalLoginUpPartyProfileViewModel> profiles, ExternalLoginUpPartyProfileViewModel removeProfile)
        {
            profiles.Remove(removeProfile);
        }

        private async Task OnEditExternalLoginUpPartyValidSubmitAsync(GeneralExternalLoginUpPartyViewModel generalExtLoginUpParty, EditContext editContext)
        {
            try
            {
                generalExtLoginUpParty.Form.Model.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalExtLoginUpParty.Form.Model.ExtendedUis.MapExtendedUisBeforeMap();
                generalExtLoginUpParty.Form.Model.ExitClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalExtLoginUpParty.Form.Model.LinkExternalUser?.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();

                if(generalExtLoginUpParty.Form.Model.UsernameType == ExternalLoginUsernameTypes.Text)
                {
                    generalExtLoginUpParty.Form.Model.HrdDomains = null;
                }

                var extLoginUpParty = generalExtLoginUpParty.Form.Model.Map<ExternalLoginUpParty>(afterMap: afterMap =>
                {
                    afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    afterMap.ExtendedUis.MapExtendedUisAfterMap();
                    afterMap.ExitClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    afterMap.LinkExternalUser = afterMap.LinkExternalUser.MapLinkExternalUserAfterMap();
                });


                if (generalExtLoginUpParty.CreateMode)
                {
                    var extLoginUpPartyResult = await UpPartyService.CreateExternalLoginUpPartyAsync(extLoginUpParty, cancellationToken: ComponentCancellationToken);
                    generalExtLoginUpParty.Form.UpdateModel(ToViewModel(extLoginUpPartyResult));
                    generalExtLoginUpParty.CreateMode = false;
                    toastService.ShowSuccess("External login application created.");
                    generalExtLoginUpParty.Name = extLoginUpPartyResult.Name;
                    generalExtLoginUpParty.DisplayName = extLoginUpPartyResult.DisplayName;
                }
                else
                {
                    if (generalExtLoginUpParty.Form.Model.Name != generalExtLoginUpParty.Form.Model.InitName)
                    {
                        extLoginUpParty.NewName = extLoginUpParty.Name;
                        extLoginUpParty.Name = generalExtLoginUpParty.Form.Model.InitName;
                    }
                    if(generalExtLoginUpParty.Form.Model.Profiles?.Count() > 0)
                    {
                        foreach(var profile in generalExtLoginUpParty.Form.Model.Profiles)
                        {
                            if(!profile.InitName.IsNullOrWhiteSpace() && profile.InitName != profile.Name)
                            {
                                var profileMap = extLoginUpParty.Profiles?.Where(p => p.Name == profile.Name).First();
                                profileMap.Name = profile.InitName;
                                profileMap.NewName = profile.Name;
                            }
                        }
                    }

                    var deleteSecret = false;
                    if (extLoginUpParty.Secret != generalExtLoginUpParty.Form.Model.SecretLoaded)
                    {
                        if (string.IsNullOrWhiteSpace(extLoginUpParty.Secret))
                        {
                            deleteSecret = true;
                        }
                        else
                        {
                            await UpPartyService.UpdateExternalLoginSecretUpPartyAsync(new ExternalLoginSecretRequest { PartyName = UpParty.Name, Secret = extLoginUpParty.Secret }, cancellationToken: ComponentCancellationToken);
                        }
                        extLoginUpParty.Secret = null;
                    }

                    var extLoginUpPartyResult = await UpPartyService.UpdateExternalLoginUpPartyAsync(extLoginUpParty, cancellationToken: ComponentCancellationToken);
                    generalExtLoginUpParty.Name = extLoginUpPartyResult.Name;
                    if (deleteSecret)
                    {
                        await UpPartyService.DeleteExternalLoginSecretUpPartyAsync(UpParty.Name, cancellationToken: ComponentCancellationToken);
                        extLoginUpPartyResult.Secret = null;
                    }
                    generalExtLoginUpParty.Form.UpdateModel(ToViewModel(extLoginUpPartyResult));
                    toastService.ShowSuccess("External login application updated.");                    
                    generalExtLoginUpParty.DisplayName = extLoginUpPartyResult.DisplayName;
                    generalExtLoginUpParty.Profiles = extLoginUpPartyResult.Profiles?.Map<List<UpPartyProfile>>();
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalExtLoginUpParty.Form.SetFieldError(nameof(generalExtLoginUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteExternalLoginUpPartyAsync(GeneralExternalLoginUpPartyViewModel generalExtLoginUpParty)
        {
            try
            {
                await UpPartyService.DeleteExternalLoginUpPartyAsync(generalExtLoginUpParty.Name, cancellationToken: ComponentCancellationToken);
                UpParties.Remove(generalExtLoginUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalExtLoginUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
