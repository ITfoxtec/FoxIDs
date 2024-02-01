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
using static ITfoxtec.Identity.IdentityConstants;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EOidcDownParty : DownPartyBase
    {
        protected List<string> responseTypeItems = new List<string>(Constants.Oidc.DefaultResponseTypes);

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
                var generalOidcDownParty = DownParty as GeneralOidcDownPartyViewModel;
                var oidcDownParty = await DownPartyService.GetOidcDownPartyAsync(DownParty.Name);
                var oidcDownSecrets = await DownPartyService.GetOidcClientSecretDownPartyAsync(DownParty.Name);
                await generalOidcDownParty.Form.InitAsync(ToViewModel(generalOidcDownParty, oidcDownParty, oidcDownSecrets));
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

        private OidcDownPartyViewModel ToViewModel(GeneralOidcDownPartyViewModel generalOidcDownParty, OidcDownParty oidcDownParty, List<OAuthClientSecretResponse> oidcDownSecrets)
        {
            return oidcDownParty.Map<OidcDownPartyViewModel>(afterMap =>
            {
                if (afterMap.Client == null)
                {
                    afterMap.Client = new OidcDownClientViewModel();
                }
                else
                {
                    afterMap.Client.ExistingSecrets = oidcDownSecrets.Select(s => new OAuthClientSecretViewModel { Name = s.Name, Info = s.Info }).ToList();
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

                    afterMap.Client.ScopesViewModel = afterMap.Client.Scopes.Map<List<OidcDownScopeViewModel>>() ?? new List<OidcDownScopeViewModel>();

                    generalOidcDownParty.ClientKeyInfoList.Clear();
                    if (afterMap.Client.ClientKeys?.Count > 0)
                    {
                        foreach (var key in afterMap.Client.ClientKeys)
                        {
                            generalOidcDownParty.ClientKeyInfoList.Add(new KeyInfoViewModel
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

                if (afterMap.Resource != null)
                {
                    generalOidcDownParty.DownPartyType = DownPartyOAuthTypes.ClientAndResource;
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void OidcDownPartyViewModelAfterInit(GeneralOidcDownPartyViewModel oidcDownParty, OidcDownPartyViewModel model)
        {
            if (oidcDownParty.CreateMode)
            {
                model.Client = new OidcDownClientViewModel();
                model.Resource = oidcDownParty.DownPartyType == DownPartyOAuthTypes.ClientAndResource ? new OAuthDownResource() : null;

                if (model.Client != null)
                {
                    model.Client.ResponseTypes.Add("code");

                    model.Client.Secrets = new List<string> { SecretGenerator.GenerateNewSecret() };

                    model.Client.ScopesViewModel.Add(new OidcDownScopeViewModel { Scope = DefaultOidcScopes.OfflineAccess });
                    model.Client.ScopesViewModel.Add(new OidcDownScopeViewModel
                    {
                        Scope = DefaultOidcScopes.Profile,
                        VoluntaryClaims = new List<OidcDownClaim>
                        {
                            new OidcDownClaim { Claim = JwtClaimTypes.Name, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.GivenName, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.MiddleName, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.FamilyName, InIdToken = true },
                            new OidcDownClaim { Claim = JwtClaimTypes.Nickname, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.PreferredUsername, InIdToken = false },
                            new OidcDownClaim { Claim = JwtClaimTypes.Birthdate, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Gender, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Picture, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Profile, InIdToken = false },
                            new OidcDownClaim { Claim = JwtClaimTypes.Website, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Locale, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.Zoneinfo, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.UpdatedAt, InIdToken = false }
                        }
                    });
                    model.Client.ScopesViewModel.Add(new OidcDownScopeViewModel { Scope = DefaultOidcScopes.Email, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Email, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.EmailVerified, InIdToken = false } } });
                    model.Client.ScopesViewModel.Add(new OidcDownScopeViewModel { Scope = DefaultOidcScopes.Address, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Address, InIdToken = true } } });
                    model.Client.ScopesViewModel.Add(new OidcDownScopeViewModel { Scope = DefaultOidcScopes.Phone, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.PhoneNumber, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.PhoneNumberVerified, InIdToken = false } } });

                    model.Client.DisableClientCredentialsGrant = true;
                }
            }
        }

        private void OnOidcDownPartyTypeChange(GeneralOidcDownPartyViewModel oidcDownParty, DownPartyOAuthTypes downPartyType)
        {
            if (downPartyType == DownPartyOAuthTypes.Client)
            {
                if (oidcDownParty.Form.Model.Resource != null)
                {
                    oidcDownParty.Form.Model.Resource = null;
                }
                if (oidcDownParty.ShowResourceTab)
                {
                    oidcDownParty.ShowClientTab = true;
                    oidcDownParty.ShowResourceTab = false;
                }
            }
            else if (downPartyType == DownPartyOAuthTypes.ClientAndResource)
            {
                if (oidcDownParty.Form.Model.Resource == null)
                {
                    oidcDownParty.Form.Model.Resource = new OAuthDownResource();
                }
            }
        }

        private void AddOidcScope(MouseEventArgs e, List<OidcDownScopeViewModel> scopesViewModel)
        {
            scopesViewModel.Add(new OidcDownScopeViewModel { ShowVoluntaryClaims = true });
        }

        private void RemoveOidcScope(MouseEventArgs e, List<OidcDownScopeViewModel> scopesViewModel, OidcDownScopeViewModel removeScope)
        {
            scopesViewModel.Remove(removeScope);
        }

        private void AddOidcScopeVoluntaryClaim(MouseEventArgs e, OidcDownScope scope)
        {
            if (scope.VoluntaryClaims == null)
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
                if(generalOidcDownParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalOidcDownParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

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
                    if (generalOidcDownParty.Form.Model.Client?.ScopesViewModel?.Count() > 0)
                    {
                        afterMap.Client.Scopes = generalOidcDownParty.Form.Model.Client.ScopesViewModel.Map<List<OidcDownScope>>();
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

                OidcDownParty oidcDownPartyResult;
                if (generalOidcDownParty.CreateMode)
                {
                    oidcDownPartyResult = await DownPartyService.CreateOidcDownPartyAsync(oidcDownParty);
                }
                else
                {
                    oidcDownPartyResult = await DownPartyService.UpdateOidcDownPartyAsync(oidcDownParty);
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

                var oauthDownSecrets = await DownPartyService.GetOAuthClientSecretDownPartyAsync(oidcDownPartyResult.Name);
                generalOidcDownParty.Form.UpdateModel(ToViewModel(generalOidcDownParty, oidcDownPartyResult, oauthDownSecrets));
                if (generalOidcDownParty.CreateMode)
                {
                    generalOidcDownParty.CreateMode = false;
                    toastService.ShowSuccess("OpenID Connect down-party created.");
                }
                else
                {
                    toastService.ShowSuccess("OpenID Connect down-party updated.");
                }
                generalOidcDownParty.Name = generalOidcDownParty.Form.Model.Name;
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
                DownParties.Remove(generalOidcDownParty);
                await OnStateHasChanged.InvokeAsync(DownParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalOidcDownParty.Form.SetError(ex.Message);
            }
        }

        private async Task OnClientCertificateFileSelectedAsync(GeneralOidcDownPartyViewModel generalOidcDownParty, IFileListEntry[] files)
        {
            if (generalOidcDownParty.Form.Model.Client.ClientKeys == null)
            {
                generalOidcDownParty.Form.Model.Client.ClientKeys = new List<JwtWithCertificateInfo>();
            }
            generalOidcDownParty.Form.ClearFieldError(nameof(generalOidcDownParty.Form.Model.Client.ClientKeys));
            foreach (var file in files)
            {
                if (file.Size > GeneralSamlUpPartyViewModel.CertificateMaxFileSize)
                {
                    generalOidcDownParty.Form.SetFieldError(nameof(generalOidcDownParty.Form.Model.Client.ClientKeys), $"That's too big. Max size: {GeneralSamlUpPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalOidcDownParty.ClientCertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    await file.Data.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwtWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        if (generalOidcDownParty.Form.Model.Client.ClientKeys.Any(k => k.X5t.Equals(jwtWithCertificateInfo.X5t, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalOidcDownParty.Form.SetFieldError(nameof(generalOidcDownParty.Form.Model.Client.ClientKeys), "Client certificates has duplicates.");
                            return;
                        }

                        generalOidcDownParty.ClientKeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = jwtWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwtWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwtWithCertificateInfo.CertificateInfo.ValidTo,
                            IsValid = jwtWithCertificateInfo.CertificateInfo.IsValid(),
                            Thumbprint = jwtWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwtWithCertificateInfo
                        });
                        generalOidcDownParty.Form.Model.Client.ClientKeys.Add(jwtWithCertificateInfo);
                    }
                    catch (Exception ex)
                    {
                        generalOidcDownParty.Form.SetFieldError(nameof(generalOidcDownParty.Form.Model.Client.ClientKeys), ex.Message);
                    }
                }

                generalOidcDownParty.ClientCertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveClientCertificate(GeneralOidcDownPartyViewModel generalOidcDownParty, KeyInfoViewModel keyInfo)
        {
            generalOidcDownParty.Form.ClearFieldError(nameof(generalOidcDownParty.Form.Model.Client.ClientKeys));
            if (generalOidcDownParty.Form.Model.Client.ClientKeys.Remove(keyInfo.Key))
            {
                generalOidcDownParty.ClientKeyInfoList.Remove(keyInfo);
            }
        }
    }
}
