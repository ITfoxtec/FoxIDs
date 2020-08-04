using FoxIDs.Client.Infrastructure;
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
            await DefaultLoadAsync();
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
                oidcDownParty.CreateMode = true;
                oidcDownParty.Edit = true;
                downParties.Insert(0, oidcDownParty);
            }
            else if (type == PartyTypes.OAuth2)
            {
                var oauthDownParty = new GeneralOAuthDownPartyViewModel();
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
                    await generalOidcDownParty.Form.InitAsync(oidcDownParty.Map<OidcDownPartyViewModel>(afterMap => 
                    {
                        if(afterMap.Resource == null)
                        {
                            afterMap.Resource = new OAuthDownResourceViewModel();
                        }
                        var defaultScopeIndex = afterMap.Resource.Scopes.FindIndex(match => match.Equals(generalOidcDownParty.Name, StringComparison.Ordinal));
                        if(defaultScopeIndex > -1)
                        {
                            afterMap.Resource.DefaultScope = true;
                            afterMap.Resource.Scopes.RemoveAt(defaultScopeIndex);
                        }
                        else
                        {
                            afterMap.Resource.DefaultScope = false;
                        }
                    }));
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (Exception ex)
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
                    await generalOAuthDownParty.Form.InitAsync(oauthDownParty.Map<OAuthDownPartyViewModel>());
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (Exception ex)
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
                                    Jwk = key
                                });
                            }
                        }
                    }));     
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (Exception ex)
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
        private void OidcDownPartyViewModelAfterInit(OidcDownPartyViewModel model)
        {
            model.Client = model.Client ?? new OidcDownClientViewModel();
            model.Resource = model.Resource ?? new OAuthDownResourceViewModel();
        }

        private void AddOidcResourceScope(MouseEventArgs e, List<OAuthDownResourceScope> resourceScopes)
        {
            resourceScopes.Add(new OAuthDownResourceScope());
        }

        private void RemoveOidcResourceScope(MouseEventArgs e, List<OAuthDownResourceScope> resourceScopes, OAuthDownResourceScope removeResourceScope)
        {
            resourceScopes.Remove(removeResourceScope);
        }

        private void AddOidcScope(MouseEventArgs e, List<OidcDownScope> scopes)
        {
            scopes.Add(new OidcDownScope());
        }

        private void RemoveOidcScope(MouseEventArgs e, List<OidcDownScope> scopes, OidcDownScope removeScope)
        {
            scopes.Remove(removeScope);
        }

        private void AddOidcScopeVoluntaryClaim(MouseEventArgs e, List<OidcDownClaim> voluntaryClaims)
        {
            if(voluntaryClaims == null)
            {
                voluntaryClaims = new List<OidcDownClaim>();
            }
            voluntaryClaims.Add(new OidcDownClaim());
        }

        private void RemoveOidcScopeVoluntaryClaim(MouseEventArgs e, List<OidcDownClaim> voluntaryClaims, OidcDownClaim removeVoluntaryClaim)
        {
            voluntaryClaims.Remove(removeVoluntaryClaim);
        }

        private async Task OnEditOidcDownPartyValidSubmitAsync(GeneralOidcDownPartyViewModel generalOidcDownParty, EditContext editContext)
        {
            try
            {
                var oidcDownParty = generalOidcDownParty.Form.Model.Map<OidcDownParty>(afterMap: afterMap =>
                {
                    if (generalOidcDownParty.Form.Model.Resource.DefaultScope && !afterMap.Resource.Scopes.Where(s => s.Equals(generalOidcDownParty.Form.Model.Name, StringComparison.Ordinal)).Any())
                    {
                        afterMap.Resource.Scopes.Add(generalOidcDownParty.Form.Model.Name);
                    }
                    if (afterMap.Resource.Scopes.Count <= 0)
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
        private async Task OnEditOAuthDownPartyValidSubmitAsync(GeneralOAuthDownPartyViewModel generalOAuthDownParty, EditContext editContext)
        {
            try
            {
                if (generalOAuthDownParty.CreateMode)
                {
                    await DownPartyService.CreateOAuthDownPartyAsync(generalOAuthDownParty.Form.Model.Map<OAuthDownParty>());
                }
                else
                {
                    await DownPartyService.UpdateOAuthDownPartyAsync(generalOAuthDownParty.Form.Model.Map<OAuthDownParty>());
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
                            Jwk = jwk
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
            if (generalSamlDownParty.Form.Model.Keys.Remove(certificateInfo.Jwk))
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
