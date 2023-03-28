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
using FoxIDs.Client.Models;
using FoxIDs.Client.Util;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EOAuthDownParty : DownPartyBase
    {
        protected List<string> responseTypeItems = new List<string>(Constants.OAuth.DefaultResponseTypes);

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
                var generalOAuthDownParty = DownParty as GeneralOAuthDownPartyViewModel;
                var oauthDownParty = await DownPartyService.GetOAuthDownPartyAsync(DownParty.Name);
                var oauthDownSecrets = await DownPartyService.GetOAuthClientSecretDownPartyAsync(DownParty.Name);
                await generalOAuthDownParty.Form.InitAsync(ToViewModel(generalOAuthDownParty, oauthDownParty, oauthDownSecrets));
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

        private OAuthDownPartyViewModel ToViewModel(GeneralOAuthDownPartyViewModel generalOAuthDownParty, OAuthDownParty oauthDownParty, List<OAuthClientSecretResponse> oauthDownSecrets)
        {
            return oauthDownParty.Map<OAuthDownPartyViewModel>(afterMap =>
            {
                if (afterMap.Client == null)
                {
                    generalOAuthDownParty.EnableClientTab = false;
                }
                else
                {
                    generalOAuthDownParty.EnableClientTab = true;
                    afterMap.Client.ExistingSecrets = oauthDownSecrets.Select(s => new OAuthClientSecretViewModel { Name = s.Name, Info = s.Info }).ToList();
                    var defaultResourceScopeIndex = afterMap.Client.ResourceScopes.FindIndex(r => r.Resource.Equals(afterMap.Name, StringComparison.Ordinal));
                    if (defaultResourceScopeIndex > -1)
                    {
                        afterMap.Client.DefaultResourceScope = true;
                        var defaultResourceScope = afterMap.Client.ResourceScopes[defaultResourceScopeIndex];
                        if (defaultResourceScope.Scopes?.Count() > 0)
                        {
                            foreach (var scope in defaultResourceScope.Scopes)
                            {
                                afterMap.Client.DefaultResourceScopeScopes.Add(scope);
                            }
                        }
                        afterMap.Client.ResourceScopes.RemoveAt(defaultResourceScopeIndex);
                    }
                    else
                    {
                        afterMap.Client.DefaultResourceScope = false;
                    }

                    afterMap.Client.ScopesViewModel = afterMap.Client.Scopes.Map<List<OAuthDownScopeViewModel>>() ?? new List<OAuthDownScopeViewModel>();
                }

                if (afterMap.Resource == null)
                {
                    generalOAuthDownParty.EnableResourceTab = false;
                }
                else
                {
                    generalOAuthDownParty.EnableResourceTab = true;
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void OAuthDownPartyViewModelAfterInit(GeneralOAuthDownPartyViewModel oauthDownParty, OAuthDownPartyViewModel model)
        {
            if (oauthDownParty.CreateMode)
            {
                if (oauthDownParty.SubPartyType == OAuthSubPartyTypes.Resource)
                {
                    oauthDownParty.EnableClientTab = false;
                    oauthDownParty.EnableResourceTab = true;
                    oauthDownParty.ShowClientTab = false;
                    oauthDownParty.ShowResourceTab = true;

                    model.Resource = new OAuthDownResource();
                }
                else if (oauthDownParty.SubPartyType == OAuthSubPartyTypes.ClientCredentialsGrant)
                {
                    oauthDownParty.EnableClientTab = true;
                    oauthDownParty.EnableResourceTab = false;
                    oauthDownParty.ShowClientTab = true;
                    oauthDownParty.ShowResourceTab = false;

                    model.Client = new OAuthDownClientViewModel();

                    model.Client.DefaultResourceScope = false;

                    model.Client.RequirePkce = false;
                    model.Client.Secrets = new List<string> { SecretGenerator.GenerateNewSecret() };

                    model.Client.ResponseTypes.Add("token");
                }
                else
                {
                    throw new NotSupportedException("OAuthSubPartyTypes not supported.");
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

                OAuthDownParty oauthDownPartyResult;
                if (generalOAuthDownParty.CreateMode)
                {
                    oauthDownPartyResult = await DownPartyService.CreateOAuthDownPartyAsync(oauthDownParty);
                }
                else
                {
                    oauthDownPartyResult = await DownPartyService.UpdateOAuthDownPartyAsync(oauthDownParty);
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

                var oauthDownSecrets = await DownPartyService.GetOAuthClientSecretDownPartyAsync(oauthDownPartyResult.Name);
                generalOAuthDownParty.Form.UpdateModel(ToViewModel(generalOAuthDownParty, oauthDownPartyResult, oauthDownSecrets));
                if (generalOAuthDownParty.CreateMode)
                {
                    generalOAuthDownParty.CreateMode = false;
                    toastService.ShowSuccess("OAuth down-party created.");
                }
                else
                {
                    toastService.ShowSuccess("OAuth down-party updated.");
                }
                generalOAuthDownParty.Name = generalOAuthDownParty.Form.Model.Name;
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
