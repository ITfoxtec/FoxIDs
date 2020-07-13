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

namespace FoxIDs.Client.Pages
{
    public partial class DownParties 
    {
        private PageEditForm<FilterPartyViewModel> downPartyFilterForm;
        private IEnumerable<DownParty> downParties = new List<DownParty>();
        private string upPartyHref;

        private string loadPartyError;
        private bool createMode;
        private bool showAdvanced;
        private bool deleteAcknowledge;
        private string currentDownPartyName;

        private Modal editOidcDownPartyModal;
        private PageEditForm<OidcDownPartyViewModel> editOidcDownPartyForm;

        private Modal editOAuthDownPartyModal;
        private PageEditForm<OAuthDownPartyViewModel> editOAuthDownPartyForm;

        private Modal editSamlDownPartyModal;
        private PageEditForm<SamlDownPartyViewModel> editSamlDownPartyForm;
        const string defaultSamlDownPartyCertificateFileStatus = "Drop certificate files here or click to select";
        const int samlDownPartyCertificateMaxFileSize = 5 * 1024 * 1024; // 5MB
        private List<CertificateInfoViewModel> certificateInfoList = new List<CertificateInfoViewModel>();
        string samlDownPartyCertificateFileStatus = defaultSamlDownPartyCertificateFileStatus;
 
        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

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
                downParties = await DownPartyService.FilterDownPartyAsync(null);
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
                downParties = await DownPartyService.FilterDownPartyAsync(downPartyFilterForm.Model.FilterName);
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

        private void ShowCreateDownPartyModal(PartyTypes type)
        {
            createMode = true;
            showAdvanced = false;
            if (type == PartyTypes.Oidc)
            {
                editOidcDownPartyForm.Init();
                editOidcDownPartyModal.Show();
            }
            else if (type == PartyTypes.OAuth2)
            {
                editOAuthDownPartyForm.Init();
                editOAuthDownPartyModal.Show();
            }
            else if (type == PartyTypes.Saml2)
            {
                editSamlDownPartyForm.Init();
                //certificateInfoList.Clear();
                editSamlDownPartyModal.Show();
            }
        }

        private async Task ShowUpdateDownPartyAsync(PartyTypes type, string downPartyName)
        {
            loadPartyError = null;
            createMode = false;
            deleteAcknowledge = false;
            showAdvanced = false;
            if (type == PartyTypes.Oidc)
            {
                try
                {
                    var loginDownParty = await DownPartyService.GetOidcDownPartyAsync(downPartyName);
                    currentDownPartyName = loginDownParty.Name;
                    editOidcDownPartyForm.Init(loginDownParty.Map<OidcDownPartyViewModel>());
                    editOidcDownPartyModal.Show();
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (Exception ex)
                {
                    loadPartyError = ex.Message;
                }
            }
            else if (type == PartyTypes.OAuth2)
            {
                try
                {
                    var loginDownParty = await DownPartyService.GetOAuthDownPartyAsync(downPartyName);
                    currentDownPartyName = loginDownParty.Name;
                    editOAuthDownPartyForm.Init(loginDownParty.Map<OAuthDownPartyViewModel>());
                    editOAuthDownPartyModal.Show();
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (Exception ex)
                {
                    loadPartyError = ex.Message;
                }
            }
            else if (type == PartyTypes.Saml2)
            {
                try
                {
                    var samlDownParty = await DownPartyService.GetSamlDownPartyAsync(downPartyName);
                    currentDownPartyName = samlDownParty.Name;
                    editSamlDownPartyForm.Init(samlDownParty.Map<SamlDownPartyViewModel>(afterMap =>
                    {
                        afterMap.AuthnRequestBinding = samlDownParty.AuthnBinding.RequestBinding;
                        afterMap.AuthnResponseBinding = samlDownParty.AuthnBinding.ResponseBinding;
                        if (!samlDownParty.LoggedOutUrl.IsNullOrEmpty())
                        {
                            afterMap.LogoutRequestBinding = samlDownParty.LogoutBinding.RequestBinding;
                            afterMap.LogoutResponseBinding = samlDownParty.LogoutBinding.ResponseBinding;
                        }

                        certificateInfoList.Clear();
                        foreach (var key in afterMap.Keys)
                        {
                            var certificate = new MTokens.JsonWebKey(key.JsonSerialize()).ToX509Certificate();
                            certificateInfoList.Add(new CertificateInfoViewModel
                            {
                                Subject = certificate.Subject,
                                ValidFrom = certificate.NotBefore,
                                ValidTo = certificate.NotAfter,
                                Thumbprint = certificate.Thumbprint,
                                Jwk = key
                            });
                        }
                    }));
                    editSamlDownPartyModal.Show();
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (Exception ex)
                {
                    loadPartyError = ex.Message;
                }
            }
        }

        private async Task OnEditOidcDownPartyValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if (createMode)
                {
                    await DownPartyService.UpdateOidcDownPartyAsync(editOidcDownPartyForm.Model.Map<OidcDownParty>());
                    await OnDownPartyFilterValidSubmitAsync(null);
                }
                else
                {
                    await DownPartyService.UpdateOidcDownPartyAsync(editOidcDownPartyForm.Model.Map<OidcDownParty>());
                }
                editOidcDownPartyModal.Hide();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    editOidcDownPartyForm.SetFieldError(nameof(editOidcDownPartyForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteOidcDownPartyAsync(string name)
        {
            try
            {
                await DownPartyService.DeleteOidcDownPartyAsync(name);
                await OnDownPartyFilterValidSubmitAsync(null);
                editOidcDownPartyModal.Hide();
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                editOidcDownPartyForm.SetError(ex.Message);
            }
        }

        private async Task OnEditOAuthDownPartyValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if (createMode)
                {
                    await DownPartyService.UpdateOAuthDownPartyAsync(editOAuthDownPartyForm.Model.Map<OAuthDownParty>());
                    await OnDownPartyFilterValidSubmitAsync(null);
                }
                else
                {
                    await DownPartyService.UpdateOAuthDownPartyAsync(editOAuthDownPartyForm.Model.Map<OAuthDownParty>());
                }
                editOAuthDownPartyModal.Hide();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    editOAuthDownPartyForm.SetFieldError(nameof(editOAuthDownPartyForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteOAuthDownPartyAsync(string name)
        {
            try
            {
                await DownPartyService.DeleteOAuthDownPartyAsync(name);
                await OnDownPartyFilterValidSubmitAsync(null);
                editOAuthDownPartyModal.Hide();
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                editOAuthDownPartyForm.SetError(ex.Message);
            }
        }

        private async Task OnSamlDownPartyCertificateFileSelectedAsync(IFileListEntry[] files)
        {
            if (editSamlDownPartyForm.Model.Keys == null)
            {
                editSamlDownPartyForm.Model.Keys = new List<JsonWebKey>();
            }
            editSamlDownPartyForm.ClearFieldError(nameof(editSamlDownPartyForm.Model.Keys));
            foreach (var file in files)
            {
                if (file.Size > samlDownPartyCertificateMaxFileSize)
                {
                    editSamlDownPartyForm.SetFieldError(nameof(editSamlDownPartyForm.Model.Keys), $"That's too big. Max size: {samlDownPartyCertificateMaxFileSize} bytes.");
                    return;
                }

                samlDownPartyCertificateFileStatus = "Loading...";

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

                        if (editSamlDownPartyForm.Model.Keys.Any(k => k.X5t.Equals(jwk.X5t, StringComparison.OrdinalIgnoreCase)))
                        {
                            editSamlDownPartyForm.SetFieldError(nameof(editSamlDownPartyForm.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        certificateInfoList.Add(new CertificateInfoViewModel
                        {
                            Subject = certificate.Subject,
                            ValidFrom = certificate.NotBefore,
                            ValidTo = certificate.NotAfter,
                            Thumbprint = certificate.Thumbprint,
                            Jwk = jwk
                        });
                        editSamlDownPartyForm.Model.Keys.Add(jwk);
                    }
                    catch (Exception ex)
                    {
                        editSamlDownPartyForm.SetFieldError(nameof(editSamlDownPartyForm.Model.Keys), ex.Message);
                    }
                }

                samlDownPartyCertificateFileStatus = defaultSamlDownPartyCertificateFileStatus;
            }
        }

        private void RemoveSamlDownPartyCertificate(CertificateInfoViewModel certificateInfo)
        {
            editSamlDownPartyForm.ClearFieldError(nameof(editSamlDownPartyForm.Model.Keys));
            if (editSamlDownPartyForm.Model.Keys.Remove(certificateInfo.Jwk))
            {
                certificateInfoList.Remove(certificateInfo);
            }
        }

        private async Task OnEditSamlDownPartyValidSubmitAsync(EditContext editContext)
        {
            try
            {
                var samlDownParty = editSamlDownPartyForm.Model.Map<SamlDownParty>(afterMap =>
                {
                    afterMap.AuthnBinding = new SamlBinding { RequestBinding = editSamlDownPartyForm.Model.AuthnRequestBinding, ResponseBinding = editSamlDownPartyForm.Model.AuthnResponseBinding };
                    if (!afterMap.LoggedOutUrl.IsNullOrEmpty())
                    {
                        afterMap.LogoutBinding = new SamlBinding { RequestBinding = editSamlDownPartyForm.Model.LogoutRequestBinding, ResponseBinding = editSamlDownPartyForm.Model.LogoutResponseBinding };
                    }
                });

                if (createMode)
                {
                    await DownPartyService.CreateSamlDownPartyAsync(samlDownParty);
                    await OnDownPartyFilterValidSubmitAsync(null);
                }
                else
                {
                    await DownPartyService.UpdateSamlDownPartyAsync(samlDownParty);
                }
                editSamlDownPartyModal.Hide();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    editSamlDownPartyForm.SetFieldError(nameof(editSamlDownPartyForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSamlDownPartyAsync(string name)
        {
            try
            {
                await DownPartyService.DeleteSamlDownPartyAsync(name);
                await OnDownPartyFilterValidSubmitAsync(null);
                editSamlDownPartyModal.Hide();
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                editSamlDownPartyForm.SetError(ex.Message);
            }
        }
    }
}
