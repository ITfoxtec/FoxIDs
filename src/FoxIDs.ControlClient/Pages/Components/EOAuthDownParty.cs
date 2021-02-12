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
using Microsoft.AspNetCore.Components.Web;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EOAuthDownParty : DownPartyBase
    {
        private void OAuthDownPartyViewModelAfterInit(GeneralOAuthDownPartyViewModel oauthDownParty, OAuthDownPartyViewModel model)
        {
            if (oauthDownParty.CreateMode)
            {
                model.Client = oauthDownParty.EnableClientTab ? new OAuthDownClientViewModel() : null;
                model.Resource = oauthDownParty.EnableResourceTab ? new OAuthDownResource() : null;

                if(model.Client != null)
                {
                    model.Client.ResponseTypes.Add("code");
                    model.Client.ScopesViewModel.Add(new OAuthDownScopeViewModel { Scope = "offline_access" });
                }
            }
        }

        private void OnOAuthDownPartyClientTabChange(GeneralOAuthDownPartyViewModel oauthDownParty, bool enableTab) => oauthDownParty.Form.Model.Client = enableTab ? new OAuthDownClientViewModel() : null;

        private void OnOAuthDownPartyResourceTabChange(GeneralOAuthDownPartyViewModel oauthDownParty, bool enableTab) => oauthDownParty.Form.Model.Resource = enableTab ? new OAuthDownResource() : null;

        private void AddOAuthScope(MouseEventArgs e, List<OAuthDownScopeViewModel> scopesViewModel)
        {
            scopesViewModel.Add(new OAuthDownScopeViewModel { ShowVoluntaryClaims = true });
        }

        private void RemoveOAuthScope(MouseEventArgs e, List<OAuthDownScopeViewModel> scopesViewModel, OAuthDownScopeViewModel removeScope)
        {
            scopesViewModel.Remove(removeScope);
        }

        private void AddOAuthScopeVoluntaryClaim(MouseEventArgs e, OAuthDownScope scope)
        {
            if (scope.VoluntaryClaims == null)
            {
                scope.VoluntaryClaims = new List<OAuthDownClaim>();
            }
            scope.VoluntaryClaims.Add(new OAuthDownClaim());
        }

        private void RemoveOAuthScopeVoluntaryClaim(MouseEventArgs e, List<OAuthDownClaim> voluntaryClaims, OAuthDownClaim removeVoluntaryClaim)
        {
            voluntaryClaims.Remove(removeVoluntaryClaim);
        }

        private void AddOAuthClaim(MouseEventArgs e, List<OAuthDownClaim> claims)
        {
            claims.Add(new OAuthDownClaim());
        }

        private void RemoveOAuthClaim(MouseEventArgs e, List<OAuthDownClaim> claims, OAuthDownClaim removeClaim)
        {
            claims.Remove(removeClaim);
        }

        private async Task OnEditOAuthDownPartyValidSubmitAsync(GeneralOAuthDownPartyViewModel generalOAuthDownParty, EditContext editContext)
        {
            try
            {
                if (generalOAuthDownParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalOAuthDownParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var oauthDownParty = generalOAuthDownParty.Form.Model.Map<OAuthDownParty>(afterMap: afterMap =>
                {
                    if (generalOAuthDownParty.Form.Model.Client?.DefaultResourceScope == true)
                    {
                        afterMap.Client.ResourceScopes.Add(new OAuthDownResourceScope { Resource = generalOAuthDownParty.Form.Model.Name, Scopes = generalOAuthDownParty.Form.Model.Client.DefaultResourceScopeScopes });
                    }
                    if (!(afterMap.Resource?.Scopes?.Count > 0))
                    {
                        afterMap.Resource = null;
                    }
                    if (generalOAuthDownParty.Form.Model.Client?.ScopesViewModel?.Count() > 0)
                    {
                        afterMap.Client.Scopes = generalOAuthDownParty.Form.Model.Client.ScopesViewModel.Map<List<OAuthDownScope>>();
                    }
                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                });

                if (generalOAuthDownParty.CreateMode)
                {
                    await DownPartyService.CreateOAuthDownPartyAsync(oauthDownParty);
                }
                else
                {
                    await DownPartyService.UpdateOAuthDownPartyAsync(oauthDownParty);
                    if (oauthDownParty.Client != null)
                    {
                        foreach (var existingSecret in generalOAuthDownParty.Form.Model.Client.ExistingSecrets.Where(s => s.Removed))
                        {
                            await DownPartyService.DeleteOAuthClientSecretDownPartyAsync(existingSecret.Name);
                        }
                    }
                }
                if (oauthDownParty.Client != null && generalOAuthDownParty.Form.Model.Client.Secrets.Count() > 0)
                {
                    await DownPartyService.CreateOAuthClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = generalOAuthDownParty.Form.Model.Name, Secrets = generalOAuthDownParty.Form.Model.Client.Secrets });
                }
                generalOAuthDownParty.Name = generalOAuthDownParty.Form.Model.Name;
                generalOAuthDownParty.Edit = false;
                await OnStateHasChanged.InvokeAsync(DownParty);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalOAuthDownParty.Form.SetFieldError(nameof(generalOAuthDownParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteOAuthDownPartyAsync(GeneralOAuthDownPartyViewModel generalOAuthDownParty)
        {
            try
            {
                await DownPartyService.DeleteOAuthDownPartyAsync(generalOAuthDownParty.Name);
                DownParties.Remove(generalOAuthDownParty);
                await OnStateHasChanged.InvokeAsync(DownParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalOAuthDownParty.Form.SetError(ex.Message);
            }
        }
    }
}
