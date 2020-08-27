﻿using FoxIDs.Client.Infrastructure;
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
using System.Security.Authentication;
using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using MTokens = Microsoft.IdentityModel.Tokens;
using System.Linq;
using BlazorInputFile;
using Microsoft.AspNetCore.Components.Web;
using System.Net.Http;

namespace FoxIDs.Client.Pages
{
    public partial class DownParties 
    {
        private PageEditForm<FilterPartyViewModel> downPartyFilterForm;
        private List<GeneralDownPartyViewModel> downParties = new List<GeneralDownPartyViewModel>();
        private string upPartyHref;
        private List<string> responseTypeItems = new List<string> { "code", "code token", "code token id_token", "token", "token id_token" };

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
            catch (AuthenticationException)
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
                oidcDownParty.EnableResourceTab = false;
                oidcDownParty.CreateMode = true;
                oidcDownParty.Edit = true;
                downParties.Insert(0, oidcDownParty);
            }
            else if (type == PartyTypes.OAuth2)
            {
                var oauthDownParty = new GeneralOAuthDownPartyViewModel();
                oauthDownParty.EnableResourceTab = false;
                oauthDownParty.CreateMode = true;
                oauthDownParty.Edit = true;
                downParties.Insert(0, oauthDownParty);
            }
            else if (type == PartyTypes.Saml2)
            {
                var samlDownParty = new GeneralSamlDownPartyViewModel();
                samlDownParty.CreateMode = true;
                samlDownParty.Edit = true;
                downParties.Insert(0, samlDownParty); 
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
                    await generalOidcDownParty.Form.InitAsync(oidcDownParty.Map<OidcDownPartyViewModel>(afterMap => 
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
                        }

                        if (afterMap.Resource == null)
                        {
                            generalOidcDownParty.EnableResourceTab = false;
                        }
                        else
                        {
                            generalOidcDownParty.EnableResourceTab = true;
                        }
                    }));
                }
                catch (AuthenticationException)
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
                        }

                        if (afterMap.Resource == null)
                        {
                            generalOAuthDownParty.EnableResourceTab = false;
                        }
                        else
                        {
                            generalOAuthDownParty.EnableResourceTab = true;
                        }
                    }));
                }
                catch (AuthenticationException)
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
                        afterMap.AuthnRequestBinding = samlDownParty.AuthnBinding.RequestBinding;
                        afterMap.AuthnResponseBinding = samlDownParty.AuthnBinding.ResponseBinding;
                        if (!samlDownParty.LoggedOutUrl.IsNullOrEmpty())
                        {
                            afterMap.LogoutRequestBinding = samlDownParty.LogoutBinding.RequestBinding;
                            afterMap.LogoutResponseBinding = samlDownParty.LogoutBinding.ResponseBinding;
                        }

                        generalSamlDownParty.CertificateInfoList.Clear();
                        if (afterMap.Keys?.Count() > 0)
                        {
                            foreach (var key in afterMap.Keys)
                            {
                                var certificate = new MTokens.JsonWebKey(key.JsonSerialize()).ToX509Certificate();
                                generalSamlDownParty.CertificateInfoList.Add(new CertificateInfoViewModel
                                {
                                    Subject = certificate.Subject,
                                    ValidFrom = certificate.NotBefore,
                                    ValidTo = certificate.NotAfter,
                                    Thumbprint = certificate.Thumbprint,
                                    Key = key
                                });
                            }
                        }
                    }));
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (HttpRequestException ex)
                {
                    downParty.Error = ex.Message;
                }
            }
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

        private void DownPartyCancel(GeneralDownPartyViewModel downParty)
        {
            if(downParty.CreateMode)
            {
                downParties.Remove(downParty);
            }
            else
            {
                downParty.Edit = false;
            }
        }

        private void AddAllowUpPartyName((IAllowUpPartyNames model, string upPartyName) arg)
        {
            if (!arg.model.AllowUpPartyNames.Where(p => p.Equals(arg.upPartyName, StringComparison.OrdinalIgnoreCase)).Any())
            {
                arg.model.AllowUpPartyNames.Add(arg.upPartyName);
            }
        }

        private void RemoveAllowUpPartyName((IAllowUpPartyNames model, string upPartyName) arg)
        {
            arg.model.AllowUpPartyNames.Remove(arg.upPartyName);
        }

        #region Oidc
        private void OidcDownPartyViewModelAfterInit(GeneralOidcDownPartyViewModel oidcDownParty, OidcDownPartyViewModel model)
        {
            if (oidcDownParty.CreateMode)
            {
                model.Client = oidcDownParty.EnableClientTab ? new OidcDownClientViewModel() : null;
                model.Resource = oidcDownParty.EnableResourceTab ? new OAuthDownResource() : null;

                model.Client.ResponseTypes.Add("code");

                model.Client.Scopes.Add(new OidcDownScope { Scope = "offline_access" });
                model.Client.Scopes.Add(new OidcDownScope { Scope = "profile", VoluntaryClaims = new List<OidcDownClaim> 
                {
                    new OidcDownClaim { Claim = "name", InIdToken = true }, new OidcDownClaim { Claim = "given_name", InIdToken = true }, new OidcDownClaim { Claim = "middle_name", InIdToken = true }, new OidcDownClaim { Claim = "family_name", InIdToken = true }, 
                    new OidcDownClaim { Claim = "nickname", InIdToken = false }, new OidcDownClaim { Claim = "preferred_username", InIdToken = false }, 
                    new OidcDownClaim { Claim = "birthdate", InIdToken = false }, new OidcDownClaim { Claim = "gender", InIdToken = false }, new OidcDownClaim { Claim = "picture", InIdToken = false }, new OidcDownClaim { Claim = "profile", InIdToken = false }, 
                    new OidcDownClaim { Claim = "website", InIdToken = false }, new OidcDownClaim { Claim = "locale", InIdToken = true }, new OidcDownClaim { Claim = "zoneinfo", InIdToken = false }, new OidcDownClaim { Claim = "updated_at", InIdToken = false }
                } });
                model.Client.Scopes.Add(new OidcDownScope { Scope = "email", VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = "email", InIdToken = true }, new OidcDownClaim { Claim = "email_verified", InIdToken = false } } });
                model.Client.Scopes.Add(new OidcDownScope { Scope = "address", VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = "address", InIdToken = true } } });
                model.Client.Scopes.Add(new OidcDownScope { Scope = "phone", VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = "phone_number", InIdToken = true }, new OidcDownClaim { Claim = "phone_number_verified", InIdToken = false } } });
            }
        }

        private void OnOidcDownPartyClientTabChange(GeneralOidcDownPartyViewModel oidcDownParty, bool enableTab) => oidcDownParty.Form.Model.Client = enableTab ? new OidcDownClientViewModel() : null;

        private void OnOidcDownPartyResourceTabChange(GeneralOidcDownPartyViewModel oidcDownParty, bool enableTab) => oidcDownParty.Form.Model.Resource = enableTab ? new OAuthDownResource() : null;

        private void AddOidcScope(MouseEventArgs e, List<OidcDownScope> scopes)
        {
            scopes.Add(new OidcDownScope());
        }

        private void RemoveOidcScope(MouseEventArgs e, List<OidcDownScope> scopes, OidcDownScope removeScope)
        {
            scopes.Remove(removeScope);
        }

        private void AddOidcScopeVoluntaryClaim(MouseEventArgs e, OidcDownScope scope)
        {
            if(scope.VoluntaryClaims == null)
            {
                scope.VoluntaryClaims = new List<OidcDownClaim>();
            }
            scope.VoluntaryClaims.Add(new OidcDownClaim());
        }

        private void RemoveOidcScopeVoluntaryClaim(MouseEventArgs e, List<OidcDownClaim> voluntaryClaims, OidcDownClaim removeVoluntaryClaim)
        {
            voluntaryClaims.Remove(removeVoluntaryClaim);
        }

        private void AddOidcClaim(MouseEventArgs e, List<OidcDownClaim> claims)
        {
            claims.Add(new OidcDownClaim());
        }

        private void RemoveOidcClaim(MouseEventArgs e, List<OidcDownClaim> claims, OidcDownClaim removeClaim)
        {
            claims.Remove(removeClaim);
        }

        private async Task OnEditOidcDownPartyValidSubmitAsync(GeneralOidcDownPartyViewModel generalOidcDownParty, EditContext editContext)
        {
            try
            {
                var oidcDownParty = generalOidcDownParty.Form.Model.Map<OidcDownParty>(afterMap: afterMap =>
                {
                    if (generalOidcDownParty.Form.Model.Client?.DefaultResourceScope == true)
                    {
                        afterMap.Client.ResourceScopes.Add(new OAuthDownResourceScope { Resource = generalOidcDownParty.Form.Model.Name, Scopes = generalOidcDownParty.Form.Model.Client.DefaultResourceScopeScopes });
                    }
                    if (!(afterMap.Resource?.Scopes?.Count > 0))
                    {
                        afterMap.Resource = null;
                    }
                });            

                if (generalOidcDownParty.CreateMode)
                {
                    await DownPartyService.CreateOidcDownPartyAsync(oidcDownParty);
                }
                else
                {
                    await DownPartyService.UpdateOidcDownPartyAsync(oidcDownParty);
                    if (oidcDownParty.Client != null)
                    {
                        foreach (var existingSecret in generalOidcDownParty.Form.Model.Client.ExistingSecrets.Where(s => s.Removed))
                        {
                            await DownPartyService.DeleteOidcClientSecretDownPartyAsync(existingSecret.Name);
                        }
                    }
                }
                if (oidcDownParty.Client != null && generalOidcDownParty.Form.Model.Client.Secrets.Count() > 0)
                {
                    await DownPartyService.CreateOidcClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = generalOidcDownParty.Form.Model.Name, Secrets = generalOidcDownParty.Form.Model.Client.Secrets });
                }

                generalOidcDownParty.Name = generalOidcDownParty.Form.Model.Name;
                generalOidcDownParty.Edit = false;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalOidcDownParty.Form.SetFieldError(nameof(generalOidcDownParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteOidcDownPartyAsync(GeneralOidcDownPartyViewModel generalOidcDownParty)
        {
            try
            {
                await DownPartyService.DeleteOidcDownPartyAsync(generalOidcDownParty.Name);
                downParties.Remove(generalOidcDownParty);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalOidcDownParty.Form.SetError(ex.Message);
            }
        }
        #endregion

        #region OAuth
        private void OAuthDownPartyViewModelAfterInit(GeneralOAuthDownPartyViewModel oauthDownParty, OAuthDownPartyViewModel model)
        {
            if (oauthDownParty.CreateMode)
            {
                model.Client = oauthDownParty.EnableClientTab ? new OAuthDownClientViewModel() : null;
                model.Resource = oauthDownParty.EnableResourceTab ? new OAuthDownResource() : null;

                model.Client.ResponseTypes.Add("code");

                model.Client.Scopes.Add(new OAuthDownScope { Scope = "offline_access" });
            }
        }

        private void OnOAuthDownPartyClientTabChange(GeneralOAuthDownPartyViewModel oauthDownParty, bool enableTab) => oauthDownParty.Form.Model.Client = enableTab ? new OAuthDownClientViewModel() : null;

        private void OnOAuthDownPartyResourceTabChange(GeneralOAuthDownPartyViewModel oauthDownParty, bool enableTab) => oauthDownParty.Form.Model.Resource = enableTab ? new OAuthDownResource() : null;

        private void AddOAuthScope(MouseEventArgs e, List<OAuthDownScope> scopes)
        {
            scopes.Add(new OAuthDownScope());
        }

        private void RemoveOAuthScope(MouseEventArgs e, List<OAuthDownScope> scopes, OAuthDownScope removeScope)
        {
            scopes.Remove(removeScope);
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
                downParties.Remove(generalOAuthDownParty);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalOAuthDownParty.Form.SetError(ex.Message);
            }
        }
        #endregion

        #region Saml
        private async Task OnSamlDownPartyCertificateFileSelectedAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, IFileListEntry[] files)
        {
            if (generalSamlDownParty.Form.Model.Keys == null)
            {
                generalSamlDownParty.Form.Model.Keys = new List<JsonWebKey>();
            }
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.Keys));
            foreach (var file in files)
            {
                if (file.Size > GeneralSamlDownPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), $"That's too big. Max size: {GeneralSamlDownPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlDownParty.CertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    await file.Data.CopyToAsync(memoryStream);

                    try
                    {
                        var certificate = new X509Certificate2(memoryStream.ToArray());
                        var msJwk = await certificate.ToJsonWebKeyAsync();
                        var jwk = msJwk.Map<JsonWebKey>(afterMap =>
                        {
                            afterMap.X5c = new List<string>(msJwk.X5c);
                        });

                        if (generalSamlDownParty.Form.Model.Keys.Any(k => k.X5t.Equals(jwk.X5t, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        generalSamlDownParty.CertificateInfoList.Add(new CertificateInfoViewModel
                        {
                            Subject = certificate.Subject,
                            ValidFrom = certificate.NotBefore,
                            ValidTo = certificate.NotAfter,
                            Thumbprint = certificate.Thumbprint,
                            Key = jwk
                        });
                        generalSamlDownParty.Form.Model.Keys.Add(jwk);
                    }
                    catch (Exception ex)
                    {
                        generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), ex.Message);
                    }
                }

                generalSamlDownParty.CertificateFileStatus = GeneralSamlDownPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveSamlDownPartyCertificate(GeneralSamlDownPartyViewModel generalSamlDownParty, CertificateInfoViewModel certificateInfo)
        {
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.Keys));
            if (generalSamlDownParty.Form.Model.Keys.Remove(certificateInfo.Key))
            {
                generalSamlDownParty.CertificateInfoList.Remove(certificateInfo);
            }
        }

        private async Task OnEditSamlDownPartyValidSubmitAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, EditContext editContext)
        {
            try
            {
                var samlDownParty = generalSamlDownParty.Form.Model.Map<SamlDownParty>(afterMap =>
                {
                    afterMap.AuthnBinding = new SamlBinding { RequestBinding = generalSamlDownParty.Form.Model.AuthnRequestBinding, ResponseBinding = generalSamlDownParty.Form.Model.AuthnResponseBinding };
                    if (!afterMap.LoggedOutUrl.IsNullOrEmpty())
                    {
                        afterMap.LogoutBinding = new SamlBinding { RequestBinding = generalSamlDownParty.Form.Model.LogoutRequestBinding, ResponseBinding = generalSamlDownParty.Form.Model.LogoutResponseBinding };
                    }
                });

                if (generalSamlDownParty.CreateMode)
                {
                    await DownPartyService.CreateSamlDownPartyAsync(samlDownParty);
                }
                else
                {
                    await DownPartyService.UpdateSamlDownPartyAsync(samlDownParty);
                }
                generalSamlDownParty.Name = generalSamlDownParty.Form.Model.Name;
                generalSamlDownParty.Edit = false;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSamlDownPartyAsync(GeneralSamlDownPartyViewModel generalSamlDownParty)
        {
            try
            {
                await DownPartyService.DeleteSamlDownPartyAsync(generalSamlDownParty.Name);
                downParties.Remove(generalSamlDownParty);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalSamlDownParty.Form.SetError(ex.Message);
            }
        } 
        #endregion
    }
}
