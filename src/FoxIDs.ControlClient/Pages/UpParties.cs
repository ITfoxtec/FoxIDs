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
using BlazorInputFile;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity;
using MTokens = Microsoft.IdentityModel.Tokens;
using System.Linq;
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
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                SetGeneralUpParties(await UpPartyService.FilterUpPartyAsync(null));
            }
            catch (AuthenticationException)
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
                upParties.Insert(0, loginUpParty);
            }
            else if (type == PartyTypes.Oidc)
            {
                throw new NotImplementedException();
            }
            else if (type == PartyTypes.Saml2)
            {
                var samlUpParty = new GeneralSamlUpPartyViewModel();
                samlUpParty.CreateMode = true;
                samlUpParty.Edit = true;
                upParties.Insert(0, samlUpParty);
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
                    await generalLoginUpParty.Form.InitAsync(loginUpParty.Map<LoginUpPartyViewModel>());
                }
                catch (AuthenticationException)
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
                throw new NotImplementedException();
            }
            else if (upParty.Type == PartyTypes.Saml2)
            {
                try
                {
                    var generalSamlUpParty = upParty as GeneralSamlUpPartyViewModel;
                    var samlUpParty = await UpPartyService.GetSamlUpPartyAsync(upParty.Name);
                    await generalSamlUpParty.Form.InitAsync(samlUpParty.Map<SamlUpPartyViewModel>(afterMap =>
                    {
                        afterMap.AuthnRequestBinding = samlUpParty.AuthnBinding.RequestBinding;
                        afterMap.AuthnResponseBinding = samlUpParty.AuthnBinding.ResponseBinding;
                        if (!samlUpParty.LogoutUrl.IsNullOrEmpty())
                        {
                            afterMap.LogoutRequestBinding = samlUpParty.LogoutBinding.RequestBinding;
                            afterMap.LogoutResponseBinding = samlUpParty.LogoutBinding.ResponseBinding;
                        }

                        generalSamlUpParty.CertificateInfoList.Clear();
                        foreach (var key in afterMap.Keys)
                        {
                            var certificate = new MTokens.JsonWebKey(key.JsonSerialize()).ToX509Certificate();
                            generalSamlUpParty.CertificateInfoList.Add(new CertificateInfoViewModel
                            {
                                Subject = certificate.Subject,
                                ValidFrom = certificate.NotBefore,
                                ValidTo = certificate.NotAfter,
                                Thumbprint = certificate.Thumbprint,
                                Jwk = key
                            });
                        }
                    }));
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (HttpRequestException ex)
                {
                    upParty.Error = ex.Message;
                }
            }
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

        private void UpPartyCancel(GeneralUpPartyViewModel upParty)
        {
            if (upParty.CreateMode)
            {
                upParties.Remove(upParty);
            }
            else
            {
                upParty.Edit = false;
            }
        }

        #region Login
        private async Task OnEditLoginUpPartyValidSubmitAsync(GeneralLoginUpPartyViewModel generalLoginUpParty, EditContext editContext)
        {
            try
            {
                if (generalLoginUpParty.CreateMode)
                {
                    await UpPartyService.UpdateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>());
                }
                else
                {
                    await UpPartyService.UpdateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>());
                }
                generalLoginUpParty.Name = generalLoginUpParty.Form.Model.Name;
                generalLoginUpParty.Edit = false;
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
                upParties.Remove(generalLoginUpParty);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalLoginUpParty.Form.SetError(ex.Message);
            }
        }
        #endregion

        #region Saml
        private async Task OnSamlUpPartyCertificateFileSelectedAsync(GeneralSamlUpPartyViewModel generalSamlUpParty, IFileListEntry[] files)
        {
            if (generalSamlUpParty.Form.Model.Keys == null)
            {
                generalSamlUpParty.Form.Model.Keys = new List<JsonWebKey>();
            }
            generalSamlUpParty.Form.ClearFieldError(nameof(generalSamlUpParty.Form.Model.Keys));
            foreach (var file in files)
            {
                if (file.Size > GeneralSamlUpPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), $"That's too big. Max size: {GeneralSamlUpPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlUpParty.CertificateFileStatus = "Loading...";

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

                        if (generalSamlUpParty.Form.Model.Keys.Any(k => k.X5t.Equals(jwk.X5t, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        generalSamlUpParty.CertificateInfoList.Add(new CertificateInfoViewModel
                        {
                            Subject = certificate.Subject,
                            ValidFrom = certificate.NotBefore,
                            ValidTo = certificate.NotAfter,
                            Thumbprint = certificate.Thumbprint,
                            Jwk = jwk
                        });
                        generalSamlUpParty.Form.Model.Keys.Add(jwk);
                    }
                    catch (Exception ex)
                    {
                        generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), ex.Message);
                    }
                }

                generalSamlUpParty.CertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveSamlUpPartyCertificate(GeneralSamlUpPartyViewModel generalSamlUpParty, CertificateInfoViewModel certificateInfo)
        {
            generalSamlUpParty.Form.ClearFieldError(nameof(generalSamlUpParty.Form.Model.Keys));
            if (generalSamlUpParty.Form.Model.Keys.Remove(certificateInfo.Jwk))
            {
                generalSamlUpParty.CertificateInfoList.Remove(certificateInfo);
            }
        }

        private async Task OnEditSamlUpPartyValidSubmitAsync(GeneralSamlUpPartyViewModel generalSamlUpParty, EditContext editContext)
        {
            try
            {
                var samlUpParty = generalSamlUpParty.Form.Model.Map<SamlUpParty>(afterMap =>
                {
                    afterMap.AuthnBinding = new SamlBinding { RequestBinding = generalSamlUpParty.Form.Model.AuthnRequestBinding, ResponseBinding = generalSamlUpParty.Form.Model.AuthnResponseBinding };
                    if (!afterMap.LogoutUrl.IsNullOrEmpty())
                    {
                        afterMap.LogoutBinding = new SamlBinding { RequestBinding = generalSamlUpParty.Form.Model.LogoutRequestBinding, ResponseBinding = generalSamlUpParty.Form.Model.LogoutResponseBinding };
                    }
                });

                if (generalSamlUpParty.CreateMode)
                {
                    await UpPartyService.CreateSamlUpPartyAsync(samlUpParty);                   
                }
                else
                {
                    await UpPartyService.UpdateSamlUpPartyAsync(samlUpParty);
                }
                generalSamlUpParty.Name = generalSamlUpParty.Form.Model.Name;
                generalSamlUpParty.Edit = false;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSamlUpPartyAsync(GeneralSamlUpPartyViewModel generalSamlUpParty)
        {
            try
            {
                await UpPartyService.DeleteSamlUpPartyAsync(generalSamlUpParty.Name);
                upParties.Remove(generalSamlUpParty);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalSamlUpParty.Form.SetError(ex.Message);
            }
        } 
        #endregion
    }
}
