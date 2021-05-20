using FoxIDs.Infrastructure;
using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity;
using MTokens = Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Net.Http;

namespace FoxIDs.Client.Pages
{
    public partial class DownParties 
    {
        private PageEditForm<FilterPartyViewModel> downPartyFilterForm;
        private List<GeneralDownPartyViewModel> downParties = new List<GeneralDownPartyViewModel>();
        private string upPartyHref;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public DownPartyService DownPartyService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            upPartyHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/upparties";
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task OnTrackSelectedAsync(Track track)
        {
            await DefaultLoadAsync();
            StateHasChanged();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                SetGeneralDownParties(await DownPartyService.FilterDownPartyAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                downPartyFilterForm.SetError(ex.Message);
            }
        }

        private async Task OnDownPartyFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralDownParties(await DownPartyService.FilterDownPartyAsync(downPartyFilterForm.Model.FilterName));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    downPartyFilterForm.SetFieldError(nameof(downPartyFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void SetGeneralDownParties(IEnumerable<DownParty> dataDownParties)
        {
            downParties.Clear();
            foreach (var dp in dataDownParties)
            {
                if (dp.Type == PartyTypes.Oidc)
                {
                    downParties.Add(new GeneralOidcDownPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.OAuth2)
                {
                    downParties.Add(new GeneralOAuthDownPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.Saml2)
                {
                    downParties.Add(new GeneralSamlDownPartyViewModel(dp));
                }
            }
        }

        private void ShowCreateDownParty(PartyTypes type)
        {
            if (type == PartyTypes.Oidc)
            {
                var oidcDownParty = new GeneralOidcDownPartyViewModel();
                oidcDownParty.CreateMode = true;
                oidcDownParty.Edit = true;
                downParties.Add(oidcDownParty);
            }
            else if (type == PartyTypes.OAuth2)
            {
                var oauthDownParty = new GeneralOAuthDownPartyViewModel();
                oauthDownParty.CreateMode = true;
                oauthDownParty.Edit = true;
                downParties.Add(oauthDownParty);
            }
            else if (type == PartyTypes.Saml2)
            {
                var samlDownParty = new GeneralSamlDownPartyViewModel();
                samlDownParty.CreateMode = true;
                samlDownParty.Edit = true;
                downParties.Add(samlDownParty); 
            }
        }

        private async Task ShowUpdateDownPartyAsync(GeneralDownPartyViewModel downParty)
        {
            downParty.CreateMode = false;
            downParty.DeleteAcknowledge = false;
            downParty.ShowAdvanced = false;
            downParty.Error = null;
            downParty.Edit = true;
            if (downParty.Type == PartyTypes.Oidc)
            {
                try
                {
                    var generalOidcDownParty = downParty as GeneralOidcDownPartyViewModel;
                    var oidcDownParty = await DownPartyService.GetOidcDownPartyAsync(downParty.Name);
                    var oidcDownSecrets = await DownPartyService.GetOidcClientSecretDownPartyAsync(downParty.Name);
                    await generalOidcDownParty.Form.InitAsync(oidcDownParty.Map((Action<OidcDownPartyViewModel>)(afterMap => 
                    {
                        if (afterMap.Client == null)
                        {
                            generalOidcDownParty.EnableClientTab = false;
                        }
                        else
                        {
                            generalOidcDownParty.EnableClientTab = true;
                            afterMap.Client.ExistingSecrets = oidcDownSecrets.Select(s => new OAuthClientSecretViewModel { Name = s.Name, Info = s.Info }).ToList();
                            var defaultResourceScopeIndex = afterMap.Client.ResourceScopes.FindIndex(r => r.Resource.Equals(generalOidcDownParty.Name, StringComparison.Ordinal));
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

                            afterMap.Client.ScopesViewModel = afterMap.Client.Scopes.Map<List<OidcDownScopeViewModel>>() ?? new List<OidcDownScopeViewModel>();
                        }

                        if (afterMap.Resource == null)
                        {
                            generalOidcDownParty.EnableResourceTab = false;
                        }
                        else
                        {
                            generalOidcDownParty.EnableResourceTab = true;
                        }

                        if (afterMap.ClaimTransforms?.Count > 0)
                        {
                            afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                        }
                    })));
                }
                catch (TokenUnavailableException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (HttpRequestException ex)
                {
                    downParty.Error = ex.Message;
                }
            }
            else if (downParty.Type == PartyTypes.OAuth2)
            {
                try
                {
                    var generalOAuthDownParty = downParty as GeneralOAuthDownPartyViewModel;
                    var oauthDownParty = await DownPartyService.GetOAuthDownPartyAsync(downParty.Name);
                    var oauthDownSecrets = await DownPartyService.GetOAuthClientSecretDownPartyAsync(downParty.Name);
                    await generalOAuthDownParty.Form.InitAsync(oauthDownParty.Map<OAuthDownPartyViewModel>(afterMap =>
                    {
                        if (afterMap.Client == null)
                        {
                            generalOAuthDownParty.EnableClientTab = false;
                        }
                        else
                        {
                            generalOAuthDownParty.EnableClientTab = true;
                            afterMap.Client.ExistingSecrets = oauthDownSecrets.Select(s => new OAuthClientSecretViewModel { Name = s.Name, Info = s.Info }).ToList();
                            var defaultResourceScopeIndex = afterMap.Client.ResourceScopes.FindIndex(r => r.Resource.Equals(generalOAuthDownParty.Name, StringComparison.Ordinal));
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
                    }));
                }
                catch (TokenUnavailableException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (HttpRequestException ex)
                {
                    downParty.Error = ex.Message;
                }
            }
            else if (downParty.Type == PartyTypes.Saml2)
            {
                try
                {
                    var generalSamlDownParty = downParty as GeneralSamlDownPartyViewModel;
                    var samlDownParty = await DownPartyService.GetSamlDownPartyAsync(downParty.Name);
                    await generalSamlDownParty.Form.InitAsync(samlDownParty.Map<SamlDownPartyViewModel>(afterMap =>
                    {
                        generalSamlDownParty.KeyInfoList.Clear();
                        if (afterMap.Keys?.Count() > 0)
                        {
                            foreach (var key in afterMap.Keys)
                            {
                                var certificate = new MTokens.JsonWebKey(key.JsonSerialize()).ToX509Certificate();
                                generalSamlDownParty.KeyInfoList.Add(new KeyInfoViewModel
                                {
                                    Subject = certificate.Subject,
                                    ValidFrom = certificate.NotBefore,
                                    ValidTo = certificate.NotAfter,
                                    Thumbprint = certificate.Thumbprint,
                                    Key = key
                                });
                            }
                        }

                        afterMap.SignMetadata = !samlDownParty.DisableSignMetadata;

                        if (afterMap.ClaimTransforms?.Count > 0)
                        {
                            afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                        }
                    }));
                }
                catch (TokenUnavailableException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (HttpRequestException ex)
                {
                    downParty.Error = ex.Message;
                }
            }
        }

        private async Task OnStateHasChangedAsync(GeneralDownPartyViewModel downParty)
        {
            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }

        private string DownPartyInfoText(GeneralDownPartyViewModel downParty)
        {
            if (downParty.Type == PartyTypes.Oidc)
            {
                return $"OpenID Connect - {downParty.Name}";
            }
            else if (downParty.Type == PartyTypes.OAuth2)
            {
                return $"OAuth 2.0 - {downParty.Name}";
            }
            else if (downParty.Type == PartyTypes.Saml2)
            {
                return $"SAML 2.0 - {downParty.Name}";
            }
            throw new NotSupportedException();
        }
    }
}
