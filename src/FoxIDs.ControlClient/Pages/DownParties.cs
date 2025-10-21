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
using Blazored.Toast.Services;
using ITfoxtec.Identity;
using FoxIDs.Client.Util;
using static ITfoxtec.Identity.IdentityConstants;
using System.Linq;
using System.Security.Claims;

namespace FoxIDs.Client.Pages
{
    public partial class DownParties 
    {
        private NewDownPartyViewModel newDownPartyModal;
        private DownPartyTestViewModel testDownPartyModal;
        private PageEditForm<FilterDownPartyViewModel> downPartyFilterForm;
        private List<GeneralDownPartyViewModel> downParties;
        private string paginationToken;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public MetadataLogic MetadataLogic { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Inject]
        public DownPartyService DownPartyService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            newDownPartyModal = new NewDownPartyViewModel();
            testDownPartyModal = new DownPartyTestViewModel();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        protected override void OnDispose()
        {
            TrackSelectedLogic.OnTrackSelectedAsync -= OnTrackSelectedAsync;
            base.OnDispose();
        }

        private async Task OnTrackSelectedAsync(Track track)
        {
            await DefaultLoadAsync();
            StateHasChanged();
        }

        private async Task DefaultLoadAsync()
        {
            downPartyFilterForm?.ClearError();
            try
            {
                SetGeneralDownParties(await DownPartyService.GetDownPartiesAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                downParties?.Clear();
                downPartyFilterForm.SetError(ex.Message);
            }
        }

        private async Task OnDownPartyFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralDownParties(await DownPartyService.GetDownPartiesAsync(downPartyFilterForm.Model.FilterName));

                if (newDownPartyModal?.IsVisible == true && newDownPartyModal.Created)
                {
                    newDownPartyModal.IsVisible = false;
                }
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

        private async Task LoadMorePartiesAsync()
        {
            try
            {
                SetGeneralDownParties(await DownPartyService.GetDownPartiesAsync(downPartyFilterForm.Model.FilterName, paginationToken: paginationToken), addParties: true);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
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

        private void SetGeneralDownParties(PaginationResponse<DownParty> dataDownParties, bool addParties = false)
        {
            var dps = new List<GeneralDownPartyViewModel>();
            foreach (var dp in dataDownParties.Data)
            {
                if (dp.Type == PartyTypes.Oidc)
                {
                    dps.Add(new GeneralOidcDownPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.OAuth2)
                {
                    dps.Add(new GeneralOAuthDownPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.Saml2)
                {
                    dps.Add(new GeneralSamlDownPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.TrackLink)
                {
                    dps.Add(new GeneralTrackLinkDownPartyViewModel(dp));
                }
            }

            if (downParties != null && addParties)
            {
                downParties.AddRange(dps);
            }
            else
            {
                downParties = dps;
            }
            paginationToken = dataDownParties.PaginationToken;
        }

        private async Task InitAndShowTestUpPartyAsync()
        {
            testDownPartyModal.Error = null;
            testDownPartyModal.DisplayName = null;
            testDownPartyModal.TestUrl = null;
            testDownPartyModal.TestExpireAt = 0;

            testDownPartyModal.Modal.Show();

            try
            {
                var downPartyTestStartResponse = await HelpersService.StartDownPartyTestAsync(new DownPartyTestStartRequest
                {
                    UpParties = await GetUpPartiesAsync(),
                    RedirectUri = $"{RouteBindingLogic.GetBaseUri().Trim('/')}/{TenantName}/applications/test".ToLower()
                });

                testDownPartyModal.DisplayName = downPartyTestStartResponse.DisplayName;
                testDownPartyModal.TestUrl = downPartyTestStartResponse.TestUrl;
                testDownPartyModal.TestExpireAt = downPartyTestStartResponse.TestExpireAt;

                var oidcDownParty = new GeneralOidcDownPartyViewModel(new DownParty { Type = PartyTypes.Oidc, Name = downPartyTestStartResponse.Name, DisplayName = downPartyTestStartResponse.DisplayName });
                downParties.Add(oidcDownParty);
                if (downParties.Count() <= 1)
                {
                    ShowUpdateDownParty(oidcDownParty);
                }
            }
            catch (TokenUnavailableException)
            {
                await(OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                testDownPartyModal.Error = ex.Message;
            }
        }

        private async Task<List<UpPartyLink>> GetUpPartiesAsync()
        {
            var ups = await UpPartyService.GetUpPartiesAsync(null);
            var upParties = new List<UpPartyLink>();
            foreach (var up in ups.Data)
            {
                upParties.Add(new UpPartyLink { Name = up.Name });
                if(up.Profiles != null)
                {
                    foreach (var profile in up.Profiles)
                    {
                        upParties.Add(new UpPartyLink { Name = up.Name, ProfileName = profile.Name });
                    }
                }
            }
            return upParties;
        }

        private void ShowUpdateDownParty(GeneralDownPartyViewModel downParty)
        {
            downParty.DeleteAcknowledge = false;
            downParty.ShowAdvanced = false;
            downParty.Error = null;
            downParty.Edit = true;
        }

        private async Task OnStateHasChangedAsync(GeneralDownPartyViewModel downParty)
        {
            await InvokeAsync(StateHasChanged);
        }

        private string GetDownPartyDisplayName(GeneralDownPartyViewModel downParty)
        {
            var displayName = downParty.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = downParty.Name;
            }

            return displayName;
        }

        private string GetDownPartyTypeLabel(GeneralDownPartyViewModel downParty)
        {
            return downParty.Type switch
            {
                PartyTypes.Oidc => "OpenID Connect",
                PartyTypes.OAuth2 => "OAuth 2.0",
                PartyTypes.Saml2 => "SAML 2.0",
                PartyTypes.TrackLink => "Environment Link",
                PartyTypes.ExternalLogin => "External API Login",
                _ => downParty.Type.ToString()
            };
        }

        private string GetDownPartyTypeLabel(PartyTypes? type)
        {
            if (type == null)
            {
                return string.Empty;
            }   

            return type switch
            {
                PartyTypes.Oidc => "OpenID Connect",
                PartyTypes.OAuth2 => "OAuth 2.0",
                PartyTypes.Saml2 => "SAML 2.0",
                PartyTypes.TrackLink => "Environment Link",
                PartyTypes.ExternalLogin => "External API Login",
                _ => type.ToString()
            };
        }

        private void ShowNewDownParty()
        {
            newDownPartyModal.Init();
            newDownPartyModal.IsVisible = true;
            StateHasChanged();
        }

        private void ChangeNewDownPartyState(string appTitle = null, PartyTypes? type = null, DownPartyOAuthTypes? oauthType = null, DownPartyOAuthClientTypes? oauthClientType = null)
        {
            newDownPartyModal.AppTitle = appTitle;
            newDownPartyModal.Type = type;
            newDownPartyModal.OAuthType = oauthType;
            newDownPartyModal.OAuthClientType = oauthClientType;
            newDownPartyModal.CreateWorking = false;
            newDownPartyModal.Created = false;
            newDownPartyModal.CreatedDownParty = null;
            newDownPartyModal.CreatedDownPartyAdded = false;
            newDownPartyModal.ShowOidcAuthorityDetails = false;
            newDownPartyModal.ShowOAuthClientAuthorityDetails = false;
            newDownPartyModal.ShowOAuthResourceAuthorityDetails = false;
            newDownPartyModal.ShowSamlMetadataDetails = false;
        }

        private void CancelOrCloseNewDownParty()
        {
            if (newDownPartyModal?.Created == true)
            {
                EnsureCreatedDownPartyAdded();
            }

            newDownPartyModal.Init();
            newDownPartyModal.IsVisible = false;
            StateHasChanged();
        }

        private async Task AddNewDownPartyAsync(GeneralDownPartyViewModel generalDownPartyViewModel)
        {
            if (generalDownPartyViewModel == null)
            {
                return;
            }

            newDownPartyModal.CreatedDownParty = generalDownPartyViewModel;
            newDownPartyModal.CreatedDownPartyAdded = false;
            newDownPartyModal.Created = true;
            newDownPartyModal.CreateWorking = false;

            await InvokeAsync(StateHasChanged);
        }

        private void EnsureCreatedDownPartyAdded()
        {
            if (newDownPartyModal?.CreatedDownParty == null || newDownPartyModal.CreatedDownPartyAdded)
            {
                return;
            }

            downParties ??= new List<GeneralDownPartyViewModel>();
            downParties.RemoveAll(dp => dp?.Name != null && dp.Name.Equals(newDownPartyModal.CreatedDownParty.Name, StringComparison.OrdinalIgnoreCase));
            downParties.Insert(0, newDownPartyModal.CreatedDownParty);
            newDownPartyModal.CreatedDownPartyAdded = true;
        }

        private void EditCreatedDownParty()
        {
            if (newDownPartyModal?.CreatedDownParty == null)
            {
                return;
            }

            EnsureCreatedDownPartyAdded();
            ShowUpdateDownParty(newDownPartyModal.CreatedDownParty);
            newDownPartyModal.Init();
            newDownPartyModal.IsVisible = false;
            StateHasChanged();
        }

        private async Task OnNewDownPartyOidcModalAfterInitAsync(NewDownPartyOidcViewModel model)
        {
            model.Name = await DownPartyService.GetNewPartyNameAsync();
        }

        private async Task OnNewDownPartyOidcModalValidSubmitAsync(NewDownPartyViewModel newDownPartyViewModel, PageEditForm<NewDownPartyOidcViewModel> newDownPartyOidcForm, EditContext editContext)
        {

            if (newDownPartyModal.OAuthClientType != DownPartyOAuthClientTypes.PublicNative)
            {
                if (!(newDownPartyOidcForm.Model.RedirectUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || newDownPartyOidcForm.Model.RedirectUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
                {
                    newDownPartyOidcForm.SetFieldError(nameof(newDownPartyOidcForm.Model.RedirectUri), "The Redirect URI must start with 'https://' or optionally 'http://' if the domain is localhost.");
                    return;
                }
            }

            try
            {
                newDownPartyViewModel.CreateWorking = true;
                if (newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.Confidential && newDownPartyOidcForm.Model.Secret.IsNullOrWhiteSpace())
                {
                    newDownPartyOidcForm.Model.Secret = SecretGenerator.GenerateNewSecret();
                }

                var oidcDownParty = newDownPartyOidcForm.Model.Map<OidcDownParty>(afterMap: afterMap =>
                {
                    afterMap.AllowUpParties = new List<UpPartyLink> { new UpPartyLink { Name = Constants.DefaultLogin.Name } };

                    afterMap.Client = new OidcDownClient
                    {
                        RedirectUris = new List<string> { newDownPartyOidcForm.Model.RedirectUri },
                        DisableAbsoluteUris = newDownPartyOidcForm.Model.DisableAbsoluteUris,
                        ResponseTypes = new List<string> { ResponseTypes.Code },
                        RequirePkce = newDownPartyModal.OAuthClientType != DownPartyOAuthClientTypes.Confidential,
                        DisableClientCredentialsGrant = true,
                        DisableClientAsTokenExchangeActor = newDownPartyModal.OAuthClientType != DownPartyOAuthClientTypes.Confidential,
                        DisableTokenExchangeGrant = newDownPartyModal.OAuthClientType != DownPartyOAuthClientTypes.Confidential,

                        Scopes = new List<OidcDownScope>
                        {
                            new OidcDownScope { Scope = DefaultOidcScopes.OfflineAccess },
                            new OidcDownScope
                            {
                                Scope = DefaultOidcScopes.Profile,
                                VoluntaryClaims = new List<OidcDownClaim>
                                {
                                    new OidcDownClaim { Claim = JwtClaimTypes.Name, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.GivenName, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.MiddleName, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.FamilyName, InIdToken = true },
                                    new OidcDownClaim { Claim = JwtClaimTypes.Nickname, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.PreferredUsername, InIdToken = true },
                                    new OidcDownClaim { Claim = JwtClaimTypes.Birthdate, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Gender, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Picture, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Profile, InIdToken = false },
                                    new OidcDownClaim { Claim = JwtClaimTypes.Website, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Locale, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.Zoneinfo, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.UpdatedAt, InIdToken = false }
                                }
                            },
                            new OidcDownScope { Scope = DefaultOidcScopes.Email, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Email, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.EmailVerified, InIdToken = false } } },
                            new OidcDownScope { Scope = DefaultOidcScopes.Address, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Address, InIdToken = true } } },
                            new OidcDownScope { Scope = DefaultOidcScopes.Phone, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.PhoneNumber, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.PhoneNumberVerified, InIdToken = false } } }
                        }
                    };

                    if (newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.Public)
                    {
                        afterMap.AllowCorsOrigins = new List<string> { newDownPartyOidcForm.Model.RedirectUri.UrlToOrigin() };
                    }
                    if (newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.PublicNative && (newDownPartyOidcForm.Model.RedirectUri.StartsWith("http://") || newDownPartyOidcForm.Model.RedirectUri.StartsWith("https://")))
                    {
                        afterMap.AllowCorsOrigins = new List<string> { newDownPartyOidcForm.Model.RedirectUri.UrlToOrigin() };
                    }
                });

                var oidcDownPartyResult = await DownPartyService.CreateOidcDownPartyAsync(oidcDownParty);
                newDownPartyOidcForm.Model.Scopes = oidcDownParty.Client.Scopes.Select(s => s.Scope).ToList();
                if (newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.Confidential)
                {
                    await DownPartyService.CreateOidcClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = oidcDownPartyResult.Name, Secrets = new List<string> { newDownPartyOidcForm.Model.Secret } });
                }
                else if (newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.Public || newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.PublicNative)
                {
                    newDownPartyOidcForm.Model.Pkce = true.ToString();
                }
                toastService.ShowSuccess("OpenID Connect authentication method created.");

                newDownPartyOidcForm.Model.Name = oidcDownPartyResult.Name;
                newDownPartyOidcForm.Model.DisplayName = oidcDownPartyResult.DisplayName;
                (var clientAuthority, _, _, _, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(newDownPartyOidcForm.Model.Name, true);
                newDownPartyOidcForm.Model.Authority = clientAuthority;
                var generalDownPartyViewModel = new GeneralOidcDownPartyViewModel(new DownParty { Type = PartyTypes.Oidc, Name = newDownPartyOidcForm.Model.Name, DisplayName = newDownPartyOidcForm.Model.DisplayName });
                await AddNewDownPartyAsync(generalDownPartyViewModel);
            }
            catch (FoxIDsApiException ex)
            {
                newDownPartyViewModel.CreateWorking = false;
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newDownPartyOidcForm.SetFieldError(nameof(newDownPartyOidcForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                newDownPartyViewModel.CreateWorking = false;
            }
        }

        private async Task OnNewDownPartyOAuthClientModalAfterInitAsync(NewDownPartyOAuthClientViewModel model)
        {
            model.Name = await DownPartyService.GetNewPartyNameAsync();
        }

        private async Task OnNewDownPartyOAuthClientModalValidSubmitAsync(NewDownPartyViewModel newDownPartyViewModel, PageEditForm<NewDownPartyOAuthClientViewModel> newDownPartyOAuthClientForm, EditContext editContext)
        {
            try
            {
                newDownPartyViewModel.CreateWorking = true;
                if (newDownPartyOAuthClientForm.Model.Secret.IsNullOrWhiteSpace())
                {
                    newDownPartyOAuthClientForm.Model.Secret = SecretGenerator.GenerateNewSecret();
                }

                var oauthDownParty = newDownPartyOAuthClientForm.Model.Map<OAuthDownParty>(afterMap: afterMap =>
                {
                    afterMap.Client = new OAuthDownClient
                    {
                        RequirePkce = false,
                        DisableClientCredentialsGrant = newDownPartyModal.OAuthType == DownPartyOAuthTypes.Resource,
                        DisableClientAsTokenExchangeActor = newDownPartyModal.OAuthClientType != DownPartyOAuthClientTypes.Confidential,
                        DisableTokenExchangeGrant = newDownPartyModal.OAuthClientType != DownPartyOAuthClientTypes.Confidential,
                    };
                });

                var oauthDownPartyResult = await DownPartyService.CreateOAuthDownPartyAsync(oauthDownParty);
                await DownPartyService.CreateOAuthClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = oauthDownPartyResult.Name, Secrets = new List<string> { newDownPartyOAuthClientForm.Model.Secret } });
                toastService.ShowSuccess("OAuth 2.0 authentication method created.");

                newDownPartyOAuthClientForm.Model.Name = oauthDownPartyResult.Name;
                newDownPartyOAuthClientForm.Model.DisplayName = oauthDownPartyResult.DisplayName;
                (var clientAuthority, _, _, _, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(newDownPartyOAuthClientForm.Model.Name, false);
                newDownPartyOAuthClientForm.Model.Authority = clientAuthority;
                var generalDownPartyViewModel = new GeneralOAuthDownPartyViewModel(new DownParty { Type = PartyTypes.OAuth2, Name = newDownPartyOAuthClientForm.Model.Name, DisplayName = newDownPartyOAuthClientForm.Model.DisplayName });
                await AddNewDownPartyAsync(generalDownPartyViewModel);
            }
            catch (FoxIDsApiException ex)
            {
                newDownPartyViewModel.CreateWorking = false;
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newDownPartyOAuthClientForm.SetFieldError(nameof(newDownPartyOAuthClientForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                newDownPartyViewModel.CreateWorking = false;
            }
        }

        private async Task OnNewDownPartyOAuthResourceModalAfterInitAsync(NewDownPartyOAuthResourceViewModel model)
        {
            model.Name = await DownPartyService.GetNewPartyNameAsync();
        }

        private async Task OnNewDownPartyOAuthResourceModalValidSubmitAsync(NewDownPartyViewModel newDownPartyViewModel, PageEditForm<NewDownPartyOAuthResourceViewModel> newDownPartyOAuthResourceForm, EditContext editContext)
        {
            try
            {
                newDownPartyViewModel.CreateWorking = true;
                var oauthDownParty = newDownPartyOAuthResourceForm.Model.Map<OAuthDownParty>(afterMap: afterMap =>
                {
                    afterMap.AllowUpParties = new List<UpPartyLink> { new UpPartyLink { Name = Constants.DefaultLogin.Name } };

                    afterMap.Resource = new OAuthDownResource
                    {
                        Scopes = newDownPartyOAuthResourceForm.Model.Scopes
                    };
                });

                var oauthDownPartyResult = await DownPartyService.CreateOAuthDownPartyAsync(oauthDownParty);
                toastService.ShowSuccess("OAuth 2.0 authentication method created.");

                newDownPartyOAuthResourceForm.Model.Name = oauthDownPartyResult.Name;
                newDownPartyOAuthResourceForm.Model.DisplayName = oauthDownPartyResult.DisplayName;
                newDownPartyOAuthResourceForm.Model.Scopes = oauthDownParty.Resource.Scopes;
                newDownPartyOAuthResourceForm.Model.ClientScopes = oauthDownParty.Resource.Scopes.Select(s => $"{oauthDownPartyResult.Name}:{s}").ToList();
                (var clientAuthority, _, _, _, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(newDownPartyOAuthResourceForm.Model.Name, false);
                newDownPartyOAuthResourceForm.Model.Authority = clientAuthority;
                var generalDownPartyViewModel = new GeneralOAuthDownPartyViewModel(new DownParty { Type = PartyTypes.OAuth2, Name = newDownPartyOAuthResourceForm.Model.Name, DisplayName = newDownPartyOAuthResourceForm.Model.DisplayName });
                await AddNewDownPartyAsync(generalDownPartyViewModel);
            }
            catch (FoxIDsApiException ex)
            {
                newDownPartyViewModel.CreateWorking = false;
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newDownPartyOAuthResourceForm.SetFieldError(nameof(newDownPartyOAuthResourceForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                newDownPartyViewModel.CreateWorking = false;
            }
        }

        private async Task OnNewDownPartySamlModalAfterInitAsync(NewDownPartySamlViewModel model)
        {
            model.Name = await DownPartyService.GetNewPartyNameAsync();

            if (!model.Name.IsNullOrWhiteSpace() && model.Issuer.IsNullOrWhiteSpace())
            {
                model.Issuer = $"uri:{model.Name}";
            }
        }

        private async Task OnNewDownPartySamlModalValidSubmitAsync(NewDownPartyViewModel newDownPartyViewModel, PageEditForm<NewDownPartySamlViewModel> newDownPartySamlForm, EditContext editContext)
        {
            try
            {
                newDownPartyViewModel.CreateWorking = true;
                // first map to add default ViewModel values
                var SamlDownPartyViewModel = newDownPartySamlForm.Model.Map<SamlDownPartyViewModel>();
                var samlDownParty = SamlDownPartyViewModel.Map<SamlDownParty>(afterMap: afterMap =>
                {
                    afterMap.AllowUpParties = new List<UpPartyLink> { new UpPartyLink { Name = Constants.DefaultLogin.Name } };
                    afterMap.Claims = new List<string> { ClaimTypes.Email, ClaimTypes.Name, ClaimTypes.GivenName, ClaimTypes.Surname };
                });

                var samlDownPartyResult = await DownPartyService.CreateSamlDownPartyAsync(samlDownParty);
                toastService.ShowSuccess("SAML 2.0 authentication method created.");

                newDownPartySamlForm.Model.Name = samlDownPartyResult.Name;
                newDownPartySamlForm.Model.Issuer = samlDownPartyResult.Issuer;
                newDownPartySamlForm.Model.DisplayName = samlDownPartyResult.DisplayName;
                var (metadata, issuer, authn, logout) = MetadataLogic.GetDownSamlMetadata(newDownPartySamlForm.Model.Name);
                newDownPartySamlForm.Model.Metadata = metadata;
                newDownPartySamlForm.Model.MetadataIssuer = issuer;
                newDownPartySamlForm.Model.MetadataAuthn = authn;
                newDownPartySamlForm.Model.MetadataLogout = logout;
                var generalDownPartyViewModel = new GeneralSamlDownPartyViewModel(new DownParty { Type = PartyTypes.Saml2, Name = newDownPartySamlForm.Model.Name, DisplayName = newDownPartySamlForm.Model.DisplayName });
                await AddNewDownPartyAsync(generalDownPartyViewModel);
            }
            catch (FoxIDsApiException ex)
            {
                newDownPartyViewModel.CreateWorking = false;
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newDownPartySamlForm.SetFieldError(nameof(newDownPartySamlForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                newDownPartyViewModel.CreateWorking = false;
            }
        }

        private void EnsureNewDownPartyOidcSummaryDefaults()
        {
            if (newDownPartyModal?.IsVisible != true || newDownPartyModal.OidcForm?.Model == null)
            {
                return;
            }

            var model = newDownPartyModal.OidcForm.Model;

            if (newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.Confidential && model.Secret.IsNullOrWhiteSpace())
            {
                model.Secret = SecretGenerator.GenerateNewSecret();
            }
            else if ((newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.Public || newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.PublicNative) && string.IsNullOrWhiteSpace(model.Pkce))
            {
                model.Pkce = bool.TrueString;
            }

            if (model.Scopes == null || !model.Scopes.Any())
            {
                model.Scopes = new List<string>
                {
                    DefaultOidcScopes.OfflineAccess,
                    DefaultOidcScopes.Profile,
                    DefaultOidcScopes.Email,
                    DefaultOidcScopes.Address,
                    DefaultOidcScopes.Phone
                };
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.OidcDiscovery = null;
                model.AuthorizeUrl = null;
                model.TokenUrl = null;
                return;
            }

            (var authority, _, var clientOidcDiscovery, var clientAuthorize, var clientToken) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(model.Name, true);
            if (string.IsNullOrWhiteSpace(model.Authority))
            {
                model.Authority = authority;
            }

            model.OidcDiscovery = clientOidcDiscovery;
            model.AuthorizeUrl = clientAuthorize;
            model.TokenUrl = clientToken;
        }

        private void EnsureNewDownPartyOAuthClientSummaryDefaults()
        {
            if (newDownPartyModal?.IsVisible != true || newDownPartyModal.OAuthClientForm?.Model == null)
            {
                return;
            }

            var model = newDownPartyModal.OAuthClientForm.Model;

            if (model.Secret.IsNullOrWhiteSpace())
            {
                model.Secret = SecretGenerator.GenerateNewSecret();
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.OidcDiscovery = null;
                model.TokenUrl = null;
                return;
            }

            (var authority, _, var clientOidcDiscovery, _, var clientToken) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(model.Name, false);
            if (string.IsNullOrWhiteSpace(model.Authority))
            {
                model.Authority = authority;
            }

            model.OidcDiscovery = clientOidcDiscovery;
            model.TokenUrl = clientToken;
        }

        private void EnsureNewDownPartyOAuthResourceSummaryDefaults()
        {
            if (newDownPartyModal?.IsVisible != true || newDownPartyModal.OAuthResourceForm?.Model == null)
            {
                return;
            }

            var model = newDownPartyModal.OAuthResourceForm.Model;

            if (!string.IsNullOrWhiteSpace(model.Name) && model.Scopes != null)
            {
                model.ScopesShow = model.Scopes.Where(s => !s.IsNullOrWhiteSpace()).Select(s => s).ToList();
                model.ClientScopes = model.Scopes.Where(s => !s.IsNullOrWhiteSpace()).Select(s => $"{model.Name}:{s}").ToList();
            }
            else
            {
                model.ScopesShow = new List<string>();
                model.ClientScopes = new List<string>();
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.OidcDiscovery = null;
                return;
            }

            (var authority, _, var resourceOidcDiscovery, _, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(model.Name, false);
            if (string.IsNullOrWhiteSpace(model.Authority))
            {
                model.Authority = authority;
            }

            model.OidcDiscovery = resourceOidcDiscovery;
        }

        private void EnsureNewDownPartySamlSummaryDefaults()
        {
            if (newDownPartyModal?.IsVisible != true || newDownPartyModal.SamlForm?.Model == null)
            {
                return;
            }

            var model = newDownPartyModal.SamlForm.Model;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.Metadata = null;
                model.MetadataIssuer = null;
                model.MetadataAuthn = null;
                model.MetadataLogout = null;
                return;
            }

            if (model.Issuer.IsNullOrWhiteSpace())
            {
                model.Issuer = $"uri:{model.Name}";
            }

            var (metadata, issuer, authn, logout) = MetadataLogic.GetDownSamlMetadata(model.Name);

            if (string.IsNullOrWhiteSpace(model.Metadata))
            {
                model.Metadata = metadata;
            }

            model.MetadataIssuer = issuer;
            model.MetadataAuthn = authn;
            model.MetadataLogout = logout;
        }
    }
}


