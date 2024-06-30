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
                var extLoginUpParty = await UpPartyService.GetExternalLoginUpPartyAsync(UpParty.Name);
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

        private async Task ExternalLoginUpPartyViewModelAfterInitAsync(GeneralExternalLoginUpPartyViewModel extLoginParty, ExternalLoginUpPartyViewModel model)
        {
            if (extLoginParty.CreateMode)
            {
                model.Name = await UpPartyService.GetNewPartyNameAsync();
            }
        }

        private async Task OnEditExternalLoginUpPartyValidSubmitAsync(GeneralExternalLoginUpPartyViewModel generalExtLoginUpParty, EditContext editContext)
        {
            try
            {
                if (generalExtLoginUpParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalExtLoginUpParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }
                if (generalExtLoginUpParty.Form.Model.LinkExternalUser?.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalExtLoginUpParty.Form.Model.LinkExternalUser.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                if(generalExtLoginUpParty.Form.Model.UsernameType == ExternalLoginUsernameTypes.Text)
                {
                    generalExtLoginUpParty.Form.Model.HrdDomains = null;
                }

                if (generalExtLoginUpParty.CreateMode)
                {
                    var extLoginUpPartyResult = await UpPartyService.CreateExternalLoginUpPartyAsync(generalExtLoginUpParty.Form.Model.Map<ExternalLoginUpParty>(afterMap: afterMap =>
                    {
                        if (afterMap.ClaimTransforms?.Count() > 0)
                        {
                            int order = 1;
                            foreach (var claimTransform in afterMap.ClaimTransforms)
                            {
                                claimTransform.Order = order++;
                            }
                        }
                    }));
                    generalExtLoginUpParty.Form.UpdateModel(ToViewModel(extLoginUpPartyResult));
                    generalExtLoginUpParty.CreateMode = false;
                    toastService.ShowSuccess("External login application created.");
                    generalExtLoginUpParty.Name = extLoginUpPartyResult.Name;
                    generalExtLoginUpParty.DisplayName = extLoginUpPartyResult.DisplayName;
                }
                else
                {
                    var loginUpParty = await UpPartyService.UpdateExternalLoginUpPartyAsync(generalExtLoginUpParty.Form.Model.Map<ExternalLoginUpParty>(afterMap: afterMap =>
                    {
                        if (afterMap.ClaimTransforms?.Count() > 0)
                        {
                            int order = 1;
                            foreach (var claimTransform in afterMap.ClaimTransforms)
                            {
                                claimTransform.Order = order++;
                            }
                        }
                    }));
                    generalExtLoginUpParty.Form.UpdateModel(ToViewModel(loginUpParty));
                    toastService.ShowSuccess("External login application updated.");
                    generalExtLoginUpParty.DisplayName = loginUpParty.DisplayName;
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
                await UpPartyService.DeleteExternalLoginUpPartyAsync(generalExtLoginUpParty.Name);
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
