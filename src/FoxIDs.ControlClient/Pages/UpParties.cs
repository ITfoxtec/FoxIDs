﻿using FoxIDs.Infrastructure;
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
using System.Net.Http;

namespace FoxIDs.Client.Pages
{
    public partial class UpParties
    {
        private PageEditForm<FilterPartyViewModel> upPartyFilterForm;
        private List<GeneralUpPartyViewModel> upParties = new List<GeneralUpPartyViewModel>();
        private string downPartyHref;
     
        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            downPartyHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/downparties";
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
                SetGeneralUpParties(await UpPartyService.FilterUpPartyAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                upPartyFilterForm.SetError(ex.Message);
            }
        }
        
        private async Task OnUpPartyFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralUpParties(await UpPartyService.FilterUpPartyAsync(upPartyFilterForm.Model.FilterName));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    upPartyFilterForm.SetFieldError(nameof(upPartyFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void SetGeneralUpParties(IEnumerable<UpParty> dataUpParties)
        {
            upParties.Clear();
            foreach (var dp in dataUpParties)
            {
                if (dp.Type == PartyTypes.Login)
                {
                    upParties.Add(new GeneralLoginUpPartyViewModel(dp));
                }
                if (dp.Type == PartyTypes.Oidc)
                {
                    upParties.Add(new GeneralOidcUpPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.Saml2)
                {
                    upParties.Add(new GeneralSamlUpPartyViewModel(dp));
                }
            }
        }

        private void ShowCreateUpParty(PartyTypes type)
        {
            if (type == PartyTypes.Login)
            {
                var loginUpParty = new GeneralLoginUpPartyViewModel();
                loginUpParty.CreateMode = true;
                loginUpParty.Edit = true;
                upParties.Add(loginUpParty);
            }
            else if (type == PartyTypes.Oidc)
            {
                var oidcUpParty = new GeneralOidcUpPartyViewModel();
                oidcUpParty.CreateMode = true;
                oidcUpParty.Edit = true;
                upParties.Add(oidcUpParty);
            }
            else if (type == PartyTypes.Saml2)
            {
                var samlUpParty = new GeneralSamlUpPartyViewModel();
                samlUpParty.CreateMode = true;
                samlUpParty.Edit = true;
                upParties.Add(samlUpParty);
            }
        }

        private async Task ShowUpdateUpPartyAsync(GeneralUpPartyViewModel upParty)
        {
            upParty.CreateMode = false;
            upParty.DeleteAcknowledge = false;
            upParty.ShowAdvanced = false;
            upParty.Error = null;
            upParty.Edit = true;
            if (upParty.Type == PartyTypes.Login)
            {
                try
                {
                    var generalLoginUpParty = upParty as GeneralLoginUpPartyViewModel;
                    var loginUpParty = await UpPartyService.GetLoginUpPartyAsync(upParty.Name);
                    await generalLoginUpParty.Form.InitAsync(loginUpParty.Map<LoginUpPartyViewModel>(afterMap: afterMap =>
                    {
                        afterMap.EnableSingleLogout = !loginUpParty.DisableSingleLogout;
                        afterMap.EnableResetPassword = !loginUpParty.DisableResetPassword;

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
                    upParty.Error = ex.Message;
                }
            }
            else if (upParty.Type == PartyTypes.Oidc)
            {
                try
                {
                    var generalOidcUpParty = upParty as GeneralOidcUpPartyViewModel;
                    var oidcUpParty = await UpPartyService.GetOidcUpPartyAsync(upParty.Name);
                    await generalOidcUpParty.Form.InitAsync(oidcUpParty.Map((Action<OidcUpPartyViewModel>)(afterMap =>
                    {
                        if (oidcUpParty.UpdateState == PartyUpdateStates.Manual)
                        {
                            afterMap.IsManual = true;
                        }

                        if (oidcUpParty.UpdateState == PartyUpdateStates.AutomaticStopped)
                        {
                            afterMap.AutomaticStopped = true;
                        }
                        else
                        {
                            afterMap.AutomaticStopped = false;
                        }

                        afterMap.EnableSingleLogout = !oidcUpParty.DisableSingleLogout;
                        if (oidcUpParty.Client != null)
                        {
                            afterMap.Client.EnableFrontChannelLogout = !oidcUpParty.Client.DisableFrontChannelLogout;
                        }

                        generalOidcUpParty.KeyInfoList.Clear();
                        foreach (var key in afterMap.Keys)
                        {
                            if (key.Kty == MTokens.JsonWebAlgorithmsKeyTypes.RSA && key.X5c?.Count >= 1)
                            {
                                var certificate = new MTokens.JsonWebKey(key.JsonSerialize()).ToX509Certificate();
                                generalOidcUpParty.KeyInfoList.Add(new KeyInfoViewModel
                                {
                                    Subject = certificate.Subject,
                                    ValidFrom = certificate.NotBefore,
                                    ValidTo = certificate.NotAfter,
                                    IsValid = certificate.IsValid(),
                                    Thumbprint = certificate.Thumbprint,
                                    KeyId = key.Kid,
                                    Key = key
                                });
                            }
                            else
                            {
                                generalOidcUpParty.KeyInfoList.Add(new KeyInfoViewModel
                                {
                                    KeyId = key.Kid,
                                    Key = key
                                });
                            }
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
                    upParty.Error = ex.Message;
                }
            }
            else if (upParty.Type == PartyTypes.Saml2)
            {
                try
                {
                    var generalSamlUpParty = upParty as GeneralSamlUpPartyViewModel;
                    var samlUpParty = await UpPartyService.GetSamlUpPartyAsync(upParty.Name);
                    await generalSamlUpParty.Form.InitAsync(samlUpParty.Map<SamlUpPartyViewModel>(afterMap =>
                    {
                        if (samlUpParty.UpdateState == PartyUpdateStates.Manual)
                        {
                            afterMap.IsManual = true;
                        }

                        if (samlUpParty.UpdateState == PartyUpdateStates.AutomaticStopped)
                        {
                            afterMap.AutomaticStopped = true;
                        }
                        else
                        {
                            afterMap.AutomaticStopped = false;
                        }

                        afterMap.EnableSingleLogout = !samlUpParty.DisableSingleLogout;

                        if (samlUpParty.AuthnContextComparison.HasValue)
                        {
                            afterMap.AuthnContextComparisonViewModel = (SamlAuthnContextComparisonTypesVievModel)Enum.Parse(typeof(SamlAuthnContextComparisonTypesVievModel), samlUpParty.AuthnContextComparison.Value.ToString());
                        }
                        else
                        {
                            afterMap.AuthnContextComparisonViewModel = SamlAuthnContextComparisonTypesVievModel.Null;
                        }                       
                        
                        generalSamlUpParty.KeyInfoList.Clear();
                        foreach (var key in afterMap.Keys)
                        {
                            var certificate = new MTokens.JsonWebKey(key.JsonSerialize()).ToX509Certificate();
                            generalSamlUpParty.KeyInfoList.Add(new KeyInfoViewModel
                            {
                                Subject = certificate.Subject,
                                ValidFrom = certificate.NotBefore,
                                ValidTo = certificate.NotAfter,
                                IsValid = certificate.IsValid(),
                                Thumbprint = certificate.Thumbprint,
                                Key = key
                            });
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
                    upParty.Error = ex.Message;
                }
            }
        }

        private async Task OnStateHasChangedAsync(GeneralUpPartyViewModel upParty)
        {
            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }

        private string UpPartyInfoText(GeneralUpPartyViewModel upParty)
        {
            if (upParty.Type == PartyTypes.Login)
            {
                return $"Login - {upParty.Name}";
            }
            else if (upParty.Type == PartyTypes.Oidc)
            {
                return $"OpenID Connect - {upParty.Name}";
            }
            else if (upParty.Type == PartyTypes.Saml2)
            {
                return $"SAML 2.0 - {upParty.Name}";
            }
            throw new NotSupportedException();
        }
    }
}
