using FoxIDs.Client.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Client.Infrastructure;
using FoxIDs.Models.Api;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EOidcUpParty : UpPartyBase
    {
        protected List<string> responseTypeItems = new List<string> (Constants.Oidc.DefaultResponseTypes);

        private void OidcUpPartyViewModelAfterInit(GeneralOidcUpPartyViewModel oidcUpParty, OidcUpPartyViewModel model)
        {
            if (oidcUpParty.CreateMode)
            {
                model.Client = new OidcUpClient();
            }
        }

        private async Task OnEditOidcUpPartyValidSubmitAsync(GeneralOidcUpPartyViewModel generalOidcUpParty, EditContext editContext)
        {
            try
            {
                if(generalOidcUpParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalOidcUpParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var oidcUpParty = generalOidcUpParty.Form.Model.Map<OidcUpParty>(afterMap: afterMap =>
                {
                    afterMap.UpdateState = PartyUpdateStates.Automatic;

                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                });

                if (generalOidcUpParty.CreateMode)
                {
                    await UpPartyService.CreateOidcUpPartyAsync(oidcUpParty);
                }
                else
                {
                    await UpPartyService.UpdateOidcUpPartyAsync(oidcUpParty);
                }

                generalOidcUpParty.Name = generalOidcUpParty.Form.Model.Name;
                generalOidcUpParty.Edit = false;
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalOidcUpParty.Form.SetFieldError(nameof(generalOidcUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteOidcUpPartyAsync(GeneralOidcUpPartyViewModel generalOidcUpParty)
        {
            try
            {
                await UpPartyService.DeleteOidcUpPartyAsync(generalOidcUpParty.Name);
                UpParties.Remove(generalOidcUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalOidcUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
