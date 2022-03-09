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
    public partial class ELoginUpParty : UpPartyBase
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
                var generalLoginUpParty = UpParty as GeneralLoginUpPartyViewModel;                
                var loginUpParty = await UpPartyService.GetLoginUpPartyAsync(UpParty.Name);
                await generalLoginUpParty.Form.InitAsync(ToViewModel(loginUpParty));
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

        private LoginUpPartyViewModel ToViewModel(LoginUpParty loginUpParty)
        {
            return loginUpParty.Map<LoginUpPartyViewModel>(afterMap: afterMap =>
            {
                afterMap.EnableSingleLogout = !loginUpParty.DisableSingleLogout;
                afterMap.EnableResetPassword = !loginUpParty.DisableResetPassword;

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });           
        }

        private void OnValidChangeEnableTwoFactorApp(GeneralLoginUpPartyViewModel generalLoginUpParty, bool enable)
        {
            if (enable && generalLoginUpParty.Form.Model.TwoFactorAppName.IsNullOrWhiteSpace())
            {
                generalLoginUpParty.Form.Model.TwoFactorAppName = TenantName;
            }

            if (!enable && generalLoginUpParty.Form.Model.RequireTwoFactor)
            {
                generalLoginUpParty.Form.Model.RequireTwoFactor = false;
            }
        }

        private void OnValidChangeRequireTwoFactor(GeneralLoginUpPartyViewModel generalLoginUpParty, bool require)
        {
            if (require && !generalLoginUpParty.Form.Model.EnableTwoFactorApp)
            {
                generalLoginUpParty.Form.Model.EnableTwoFactorApp = true;
            }
        }

        private async Task OnEditLoginUpPartyValidSubmitAsync(GeneralLoginUpPartyViewModel generalLoginUpParty, EditContext editContext)
        {
            try
            {
                if (generalLoginUpParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalLoginUpParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                if (generalLoginUpParty.CreateMode)
                {
                    var loginUpPartyResult = await UpPartyService.CreateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>(afterMap: afterMap =>
                    {
                        afterMap.DisableSingleLogout = !generalLoginUpParty.Form.Model.EnableSingleLogout;
                        afterMap.DisableResetPassword = !generalLoginUpParty.Form.Model.EnableResetPassword;

                        if (afterMap.ClaimTransforms?.Count() > 0)
                        {
                            int order = 1;
                            foreach (var claimTransform in afterMap.ClaimTransforms)
                            {
                                claimTransform.Order = order++;
                            }
                        }
                    }));
                    generalLoginUpParty.Form.UpdateModel(ToViewModel(loginUpPartyResult));
                    generalLoginUpParty.CreateMode = false;
                    toastService.ShowSuccess("Login Up-party created.", "SUCCESS");
                }
                else
                {
                    var loginUpParty = await UpPartyService.UpdateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>(afterMap: afterMap =>
                    {
                        afterMap.DisableSingleLogout = !generalLoginUpParty.Form.Model.EnableSingleLogout;
                        afterMap.DisableResetPassword = !generalLoginUpParty.Form.Model.EnableResetPassword;

                        if (afterMap.ClaimTransforms?.Count() > 0)
                        {
                            int order = 1;
                            foreach (var claimTransform in afterMap.ClaimTransforms)
                            {
                                claimTransform.Order = order++;
                            }
                        }
                    }));
                    generalLoginUpParty.Form.UpdateModel(ToViewModel(loginUpParty));
                    toastService.ShowSuccess("Login Up-party updated.", "SUCCESS");
                }
                generalLoginUpParty.Name = generalLoginUpParty.Form.Model.Name;             
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalLoginUpParty.Form.SetFieldError(nameof(generalLoginUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteLoginUpPartyAsync(GeneralLoginUpPartyViewModel generalLoginUpParty)
        {
            try
            {
                await UpPartyService.DeleteLoginUpPartyAsync(generalLoginUpParty.Name);
                UpParties.Remove(generalLoginUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalLoginUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
