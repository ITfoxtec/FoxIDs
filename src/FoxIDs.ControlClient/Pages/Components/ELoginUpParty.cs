using FoxIDs.Client.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Client.Infrastructure;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ELoginUpParty : UpPartyBase
    {
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
                    await UpPartyService.CreateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>(afterMap: afterMap =>
                    {
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
                }
                else
                {
                    await UpPartyService.UpdateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>(afterMap: afterMap =>
                    {
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
                }
                generalLoginUpParty.Name = generalLoginUpParty.Form.Model.Name;
                generalLoginUpParty.Edit = false;
                await OnStateHasChanged.InvokeAsync(UpParty);
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
