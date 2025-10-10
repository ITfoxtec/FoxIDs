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
using Microsoft.AspNetCore.WebUtilities;
using System.IO;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EOidcDownParty : DownPartyBase
    {
        protected List<string> responseTypeItems = new List<string>(Constants.Oidc.DefaultResponseTypes);

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadAsync();            
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

        private void EnsureOidcDownPartySummaryDefaults(GeneralOidcDownPartyViewModel generalOidcDownParty)
        {
            var model = generalOidcDownParty?.Form?.Model;
            if (model == null)
            {
                return;
            }

            if (model.Name.IsNullOrWhiteSpace())
            {
                model.Authority = null;
                model.OidcDiscovery = null;
                model.AuthorizeUrl = null;
                model.TokenUrl = null;
                model.ResourceAuthority = null;
                model.ResourceOidcDiscovery = null;
                generalOidcDownParty.ShowAuthorityDetails = false;
                generalOidcDownParty.ShowResourceAuthorityDetails = false;
                return;
            }

            if (generalOidcDownParty.DownPartyType == DownPartyOAuthTypes.Client || generalOidcDownParty.DownPartyType == DownPartyOAuthTypes.ClientAndResource)
            {
                var (authority, partyAuthority, oidcDiscovery, authorize, token) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(model.Name, true, model.PartyBindingPattern);
                model.Authority = authority;
                model.PartyAuthority = partyAuthority;  
                model.OidcDiscovery = oidcDiscovery;
                model.AuthorizeUrl = authorize;
                model.TokenUrl = token;
            }
            else
            {
                model.Authority = null;
                model.OidcDiscovery = null;
                model.AuthorizeUrl = null;
                model.TokenUrl = null;
                generalOidcDownParty.ShowAuthorityDetails = false;
            }

            if (generalOidcDownParty.DownPartyType == DownPartyOAuthTypes.ClientAndResource)
            {
                var (resourceAuthority, partyResourceAuthority, resourceOidcDiscovery, _, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(model.Name, false, model.PartyBindingPattern);
                model.ResourceAuthority = resourceAuthority;
                model.PartyResourceAuthority = partyResourceAuthority;
                model.ResourceOidcDiscovery = resourceOidcDiscovery;
            }
            else
            {
                model.ResourceAuthority = null;
                model.ResourceOidcDiscovery = null;
                generalOidcDownParty.ShowResourceAuthorityDetails = false;
            }
        }

        private OidcDownPartyViewModel ToViewModel(GeneralOidcDownPartyViewModel generalOidcDownParty, OidcDownParty oidcDownParty, List<OAuthClientSecretResponse> oidcDownSecrets)
        {
            return oidcDownParty.Map<OidcDownPartyViewModel>(afterMap =>
            {
                afterMap.InitName = afterMap.Name;

                if (afterMap.DisplayName.IsNullOrWhiteSpace())
                {
                    afterMap.DisplayName = afterMap.Name;
                }

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
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapOAuthClaimTransforms();
                }
            });
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
                generalOidcDownParty.Form.Model.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();

                var oidcDownParty = generalOidcDownParty.Form.Model.Map<OidcDownParty>(afterMap: afterMap =>
                {
                    if (generalOidcDownParty.Form.Model.Name != generalOidcDownParty.Form.Model.InitName)
                    {
                        afterMap.NewName = afterMap.Name;
                        afterMap.Name = generalOidcDownParty.Form.Model.InitName;
                    }

                    if (generalOidcDownParty.Form.Model.Client?.DefaultResourceScope == true && !generalOidcDownParty.Form.Model.Name.IsNullOrWhiteSpace())
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
                    afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    if (afterMap.IsTest == true)
                    { 
                        if (generalOidcDownParty.Form.Model.TestExpireInSeconds == 0 || generalOidcDownParty.Form.Model.TestExpireInSeconds != 15)
                        {
                            afterMap.TestExpireInSeconds = generalOidcDownParty.Form.Model.TestExpireInSeconds;
                        }
                        else
                        {
                            afterMap.TestExpireInSeconds = null;
                        }
                    }
                });
             
                var oidcDownPartyResult = await DownPartyService.UpdateOidcDownPartyAsync(oidcDownParty);
                generalOidcDownParty.Name = oidcDownPartyResult.Name;
                if (oidcDownParty.Client != null)
                {
                    foreach (var existingSecret in generalOidcDownParty.Form.Model.Client.ExistingSecrets.Where(s => s.Removed))
                    {
                        await DownPartyService.DeleteOidcClientSecretDownPartyAsync(existingSecret.Name);
                    }
                }
                if (oidcDownParty.Client != null && generalOidcDownParty.Form.Model.Client.Secrets.Count() > 0)
                {
                    await DownPartyService.CreateOidcClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = oidcDownPartyResult.Name, Secrets = generalOidcDownParty.Form.Model.Client.Secrets });
                }

                var oauthDownSecrets = await DownPartyService.GetOAuthClientSecretDownPartyAsync(oidcDownPartyResult.Name);
                generalOidcDownParty.Form.UpdateModel(ToViewModel(generalOidcDownParty, oidcDownPartyResult, oauthDownSecrets));
                toastService.ShowSuccess("OpenID Connect authentication method updated.");
                generalOidcDownParty.DisplayName = oidcDownPartyResult.DisplayName;
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

        private async Task OnClientCertificateFileSelectedAsync(GeneralOidcDownPartyViewModel generalOidcDownParty, InputFileChangeEventArgs e)
        {
            if (generalOidcDownParty.Form.Model.Client.ClientKeys == null)
            {
                generalOidcDownParty.Form.Model.Client.ClientKeys = new List<JwkWithCertificateInfo>();
            }
            generalOidcDownParty.Form.ClearFieldError(nameof(generalOidcDownParty.Form.Model.Client.ClientKeys));
            foreach (var file in e.GetMultipleFiles())
            {
                if (file.Size > GeneralSamlUpPartyViewModel.CertificateMaxFileSize)
                {
                    generalOidcDownParty.Form.SetFieldError(nameof(generalOidcDownParty.Form.Model.Client.ClientKeys), $"That's too big. Max size: {GeneralSamlUpPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalOidcDownParty.ClientCertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    using var fileStream = file.OpenReadStream();
                    await fileStream.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwkWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        if (generalOidcDownParty.Form.Model.Client.ClientKeys.Any(k => k.Kid.Equals(jwkWithCertificateInfo.Kid, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalOidcDownParty.Form.SetFieldError(nameof(generalOidcDownParty.Form.Model.Client.ClientKeys), "Client certificates has duplicates.");
                            return;
                        }

                        generalOidcDownParty.ClientKeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = jwkWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwkWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwkWithCertificateInfo.CertificateInfo.ValidTo,
                            IsValid = jwkWithCertificateInfo.CertificateInfo.IsValid(),
                            Thumbprint = jwkWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwkWithCertificateInfo
                        });
                        generalOidcDownParty.Form.Model.Client.ClientKeys.Add(jwkWithCertificateInfo);
                    }
                    catch (TokenUnavailableException)
                    {
                        await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
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
