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
using FoxIDs.Client.Util;
using BlazorInputFile;
using Microsoft.AspNetCore.WebUtilities;
using System.IO;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EOAuthDownParty : DownPartyBase
    {
        protected List<string> responseTypeItems = new List<string>(Constants.OAuth.DefaultResponseTypes);

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
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
                afterMap.InitName = afterMap.Name;

                if (afterMap.DisplayName.IsNullOrWhiteSpace())
                {
                    afterMap.DisplayName = afterMap.Name;
                }

                if (afterMap.Client != null && afterMap.Resource != null)
                {
                    generalOAuthDownParty.DownPartyType = DownPartyOAuthTypes.ClientAndResource;
                }
                else
                {
                    if (afterMap.Client != null)
                    {
                        generalOAuthDownParty.DownPartyType = DownPartyOAuthTypes.Client;
                    }
                    else
                    {
                        generalOAuthDownParty.DownPartyType = DownPartyOAuthTypes.Resource;
                    }
                }

                if (afterMap.Client != null)
                {
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

                    generalOAuthDownParty.ClientKeyInfoList.Clear();
                    if (afterMap.Client.ClientKeys?.Count > 0)
                    {
                        foreach (var key in afterMap.Client.ClientKeys)
                        {
                            generalOAuthDownParty.ClientKeyInfoList.Add(new KeyInfoViewModel
                            {
                                Subject = key.CertificateInfo.Subject,
                                ValidFrom = key.CertificateInfo.ValidFrom,
                                ValidTo = key.CertificateInfo.ValidTo,
                                IsValid = key.CertificateInfo.IsValid(),
                                Thumbprint = key.CertificateInfo.Thumbprint,
                                Key = key
                            });
                        }
                    }
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void OAuthDownPartyViewModelAfterInit(GeneralOAuthDownPartyViewModel oauthDownParty, OAuthDownPartyViewModel model)
        {

            if (oauthDownParty.DownPartyType == DownPartyOAuthTypes.Resource)
            {
                oauthDownParty.ShowClientTab = false;
                oauthDownParty.ShowResourceTab = true;
            }
            else
            {
                oauthDownParty.ShowClientTab = true;
                oauthDownParty.ShowResourceTab = false;
            }
        }

        private OAuthDownClientViewModel GetDefaultOAuthClientViewModel()
        {
            var client = new OAuthDownClientViewModel();
            client.DefaultResourceScope = false;
            client.RequirePkce = false;
            client.Secrets = new List<string> { SecretGenerator.GenerateNewSecret() };
            return client;
        }

        private void OnOAuthDownPartyTypeChange(GeneralOAuthDownPartyViewModel oauthDownParty, DownPartyOAuthTypes downPartyType)
        {
            if (downPartyType == DownPartyOAuthTypes.Client)
            {
                if (oauthDownParty.Form.Model.Client == null)
                {
                    oauthDownParty.Form.Model.Client = GetDefaultOAuthClientViewModel();
                }
                if (oauthDownParty.Form.Model.Resource != null)
                {
                    oauthDownParty.Form.Model.Resource = null;
                }
                if (oauthDownParty.ShowResourceTab)
                {
                    oauthDownParty.ShowClientTab = true;
                    oauthDownParty.ShowResourceTab = false;
                }
            }
            else if (downPartyType == DownPartyOAuthTypes.Resource)
            {
                if (oauthDownParty.Form.Model.Client != null)
                {
                    oauthDownParty.Form.Model.Client = null;
                }
                if (oauthDownParty.Form.Model.Resource == null)
                {
                    oauthDownParty.Form.Model.Resource = new OAuthDownResource();
                }
                if (oauthDownParty.ShowClientTab)
                {
                    oauthDownParty.ShowClientTab = false;
                    oauthDownParty.ShowResourceTab = true;
                }
            }
            else if (downPartyType == DownPartyOAuthTypes.ClientAndResource)
            {
                if (oauthDownParty.Form.Model.Client == null)
                {
                    oauthDownParty.Form.Model.Client = GetDefaultOAuthClientViewModel();
                }
                if (oauthDownParty.Form.Model.Resource == null)
                {
                    oauthDownParty.Form.Model.Resource = new OAuthDownResource();
                }
            }
        }

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
                    if (generalOAuthDownParty.Form.Model.Name != generalOAuthDownParty.Form.Model.InitName)
                    {
                        afterMap.NewName = afterMap.Name;
                        afterMap.Name = generalOAuthDownParty.Form.Model.InitName;
                    }

                    if (generalOAuthDownParty.Form.Model.Client?.DefaultResourceScope == true && !generalOAuthDownParty.Form.Model.Name.IsNullOrWhiteSpace())
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

                var oauthDownPartyResult = await DownPartyService.UpdateOAuthDownPartyAsync(oauthDownParty);
                generalOAuthDownParty.Name = oauthDownPartyResult.Name;
                if (oauthDownParty.Client != null)
                {
                    foreach (var existingSecret in generalOAuthDownParty.Form.Model.Client.ExistingSecrets.Where(s => s.Removed))
                    {
                        await DownPartyService.DeleteOAuthClientSecretDownPartyAsync(existingSecret.Name);
                    }
                }
                if (oauthDownParty.Client != null && generalOAuthDownParty.Form.Model.Client.Secrets.Count() > 0)
                {
                    await DownPartyService.CreateOAuthClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = oauthDownPartyResult.Name, Secrets = generalOAuthDownParty.Form.Model.Client.Secrets });
                }

                var oauthDownSecrets = await DownPartyService.GetOAuthClientSecretDownPartyAsync(oauthDownPartyResult.Name);
                generalOAuthDownParty.Form.UpdateModel(ToViewModel(generalOAuthDownParty, oauthDownPartyResult, oauthDownSecrets));
                toastService.ShowSuccess("OAuth 2.0 authentication method updated.");
                generalOAuthDownParty.DisplayName = oauthDownPartyResult.DisplayName;
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

        private async Task OnClientCertificateFileSelectedAsync(GeneralOAuthDownPartyViewModel generalOAuthDownParty, IFileListEntry[] files)
        {
            if (generalOAuthDownParty.Form.Model.Client.ClientKeys == null)
            {
                generalOAuthDownParty.Form.Model.Client.ClientKeys = new List<JwkWithCertificateInfo>();
            }
            generalOAuthDownParty.Form.ClearFieldError(nameof(generalOAuthDownParty.Form.Model.Client.ClientKeys));
            foreach (var file in files)
            {
                if (file.Size > GeneralSamlUpPartyViewModel.CertificateMaxFileSize)
                {
                    generalOAuthDownParty.Form.SetFieldError(nameof(generalOAuthDownParty.Form.Model.Client.ClientKeys), $"That's too big. Max size: {GeneralSamlUpPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalOAuthDownParty.ClientCertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    await file.Data.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwkWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        if (generalOAuthDownParty.Form.Model.Client.ClientKeys.Any(k => k.Kid.Equals(jwkWithCertificateInfo.Kid, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalOAuthDownParty.Form.SetFieldError(nameof(generalOAuthDownParty.Form.Model.Client.ClientKeys), "Client certificates has duplicates.");
                            return;
                        }

                        generalOAuthDownParty.ClientKeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = jwkWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwkWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwkWithCertificateInfo.CertificateInfo.ValidTo,
                            IsValid = jwkWithCertificateInfo.CertificateInfo.IsValid(),
                            Thumbprint = jwkWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwkWithCertificateInfo
                        });
                        generalOAuthDownParty.Form.Model.Client.ClientKeys.Add(jwkWithCertificateInfo);
                    }
                    catch (Exception ex)
                    {
                        generalOAuthDownParty.Form.SetFieldError(nameof(generalOAuthDownParty.Form.Model.Client.ClientKeys), ex.Message);
                    }
                }

                generalOAuthDownParty.ClientCertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveClientCertificate(GeneralOAuthDownPartyViewModel generalOAuthDownParty, KeyInfoViewModel keyInfo)
        {
            generalOAuthDownParty.Form.ClearFieldError(nameof(generalOAuthDownParty.Form.Model.Client.ClientKeys));
            if (generalOAuthDownParty.Form.Model.Client.ClientKeys.Remove(keyInfo.Key))
            {
                generalOAuthDownParty.ClientKeyInfoList.Remove(keyInfo);
            }
        }
    }
}
