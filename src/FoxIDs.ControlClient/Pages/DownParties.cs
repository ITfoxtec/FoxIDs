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
        private PageEditForm<FilterDownPartyViewModel> downPartyFilterForm;
        private List<GeneralDownPartyViewModel> downParties = new List<GeneralDownPartyViewModel>();

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public MetadataLogic MetadataLogic { get; set; }

        [Inject]
        public DownPartyService DownPartyService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            newDownPartyModal = new NewDownPartyViewModel();
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
                else if (dp.Type == PartyTypes.TrackLink)
                {
                    downParties.Add(new GeneralTrackLinkDownPartyViewModel(dp));
                }
            }
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

        private string DownPartyInfoText(GeneralDownPartyViewModel downParty)
        {
            if (downParty.Type == PartyTypes.Oidc)
            {
                return $"{downParty.DisplayName ?? downParty.Name} (OpenID Connect)";
            }
            else if (downParty.Type == PartyTypes.OAuth2)
            {
                return $"{downParty.DisplayName ?? downParty.Name} (OAuth 2.0)";
            }
            else if (downParty.Type == PartyTypes.Saml2)
            {
                return $"{downParty.DisplayName ?? downParty.Name} (SAML 2.0)";
            } 
            else if (downParty.Type == PartyTypes.TrackLink)
            {
                return $"{downParty.DisplayName ?? downParty.Name} (Environment Link)";
            }

            throw new NotSupportedException();
        }

        private void ShowNewDownParty()
        {
            newDownPartyModal.Init();
            newDownPartyModal.Modal.Show();
        }

        private void ChangeNewDownPartyState(string appTitle = null, PartyTypes? type = null, DownPartyOAuthTypes? oauthType = null, DownPartyOAuthClientTypes? oauthClientType = null)
        {
            newDownPartyModal.AppTitle = appTitle;
            newDownPartyModal.Type = type;
            newDownPartyModal.OAuthType = oauthType;
            newDownPartyModal.OAuthClientType = oauthClientType;
        }

        private async Task OnNewDownPartyOidcModalValidSubmitAsync(NewDownPartyViewModel newDownPartyViewModel, PageEditForm<NewDownPartyOidcViewModel> newDownPartyOidcForm, EditContext editContext)
        {
            try
            {
                if (newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.Confidential)
                {
                    newDownPartyOidcForm.Model.Secret = SecretGenerator.GenerateNewSecret();
                }

                var oidcDownParty = newDownPartyOidcForm.Model.Map<OidcDownParty>(afterMap: afterMap =>
                {
                    afterMap.AllowUpPartyNames = new List<string> { Constants.DefaultLogin.Name };

                    afterMap.Client = new OidcDownClient
                    {
                        RedirectUris = newDownPartyOidcForm.Model.RedirectUris,
                        DisableAbsoluteUris = newDownPartyOidcForm.Model.DisableAbsoluteUris,
                        ResponseTypes = new List<string> { "code" },
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
                                    new OidcDownClaim { Claim = JwtClaimTypes.Nickname, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.PreferredUsername, InIdToken = false },
                                    new OidcDownClaim { Claim = JwtClaimTypes.Birthdate, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Gender, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Picture, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Profile, InIdToken = false },
                                    new OidcDownClaim { Claim = JwtClaimTypes.Website, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Locale, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.Zoneinfo, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.UpdatedAt, InIdToken = false }
                                }
                            },
                            new OidcDownScope { Scope = DefaultOidcScopes.Email, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Email, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.EmailVerified, InIdToken = false } } },
                            new OidcDownScope { Scope = DefaultOidcScopes.Address, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Address, InIdToken = true } } },
                            new OidcDownScope { Scope = DefaultOidcScopes.Phone, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.PhoneNumber, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.PhoneNumberVerified, InIdToken = false } } }
                        }
                    };
                });

                var oidcDownPartyResult = await DownPartyService.CreateOidcDownPartyAsync(oidcDownParty);
                if (newDownPartyModal.OAuthClientType == DownPartyOAuthClientTypes.Confidential)
                {
                    await DownPartyService.CreateOidcClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = oidcDownPartyResult.Name, Secrets = new List<string> { newDownPartyOidcForm.Model.Secret } });
                }
                toastService.ShowSuccess("OpenID Connect authentication method created.");

                newDownPartyOidcForm.Model.Name = oidcDownPartyResult.Name;
                newDownPartyOidcForm.Model.DisplayName = oidcDownPartyResult.DisplayName;
                (var clientAuthority, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(newDownPartyOidcForm.Model.Name, true);
                newDownPartyOidcForm.Model.Authority = clientAuthority;
                downParties.Add(new GeneralOidcDownPartyViewModel(new DownParty { Type = PartyTypes.Oidc, Name = newDownPartyOidcForm.Model.Name, DisplayName = newDownPartyOidcForm.Model.DisplayName }));
                newDownPartyViewModel.Created = true;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newDownPartyOidcForm.SetFieldError(nameof(newDownPartyOidcForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task OnNewDownPartyOAuthClientModalValidSubmitAsync(NewDownPartyViewModel newDownPartyViewModel, PageEditForm<NewDownPartyOAuthClientViewModel> newDownPartyOAuthClientForm, EditContext editContext)
        {
            try
            {
                newDownPartyOAuthClientForm.Model.Secret = SecretGenerator.GenerateNewSecret();

                var oauthDownParty = newDownPartyOAuthClientForm.Model.Map<OAuthDownParty>(afterMap: afterMap =>
                {
                    afterMap.AllowUpPartyNames = new List<string> { Constants.DefaultLogin.Name };

                    afterMap.Client = new OAuthDownClient
                    {
                        RequirePkce = false,
                    };
                });

                var oauthDownPartyResult = await DownPartyService.CreateOAuthDownPartyAsync(oauthDownParty);
                await DownPartyService.CreateOAuthClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = oauthDownPartyResult.Name, Secrets = new List<string> { newDownPartyOAuthClientForm.Model.Secret } });
                toastService.ShowSuccess("OAuth 2.0 authentication method created.");

                newDownPartyOAuthClientForm.Model.Name = oauthDownPartyResult.Name;
                newDownPartyOAuthClientForm.Model.DisplayName = oauthDownPartyResult.DisplayName;
                (var clientAuthority, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(newDownPartyOAuthClientForm.Model.Name, false);
                newDownPartyOAuthClientForm.Model.Authority = clientAuthority;
                downParties.Add(new GeneralOAuthDownPartyViewModel(new DownParty { Type = PartyTypes.OAuth2, Name = newDownPartyOAuthClientForm.Model.Name, DisplayName = newDownPartyOAuthClientForm.Model.DisplayName }));
                newDownPartyViewModel.Created = true;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newDownPartyOAuthClientForm.SetFieldError(nameof(newDownPartyOAuthClientForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task OnNewDownPartyOAuthResourceModalValidSubmitAsync(NewDownPartyViewModel newDownPartyViewModel, PageEditForm<NewDownPartyOAuthResourceViewModel> newDownPartyOAuthResourceForm, EditContext editContext)
        {
            try
            {
                var oauthDownParty = newDownPartyOAuthResourceForm.Model.Map<OAuthDownParty>(afterMap: afterMap =>
                {
                    afterMap.AllowUpPartyNames = new List<string> { Constants.DefaultLogin.Name };

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
                (var clientAuthority, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(newDownPartyOAuthResourceForm.Model.Name, false);
                newDownPartyOAuthResourceForm.Model.Authority = clientAuthority;
                downParties.Add(new GeneralOAuthDownPartyViewModel(new DownParty { Type = PartyTypes.OAuth2, Name = newDownPartyOAuthResourceForm.Model.Name, DisplayName = newDownPartyOAuthResourceForm.Model.DisplayName }));
                newDownPartyViewModel.Created = true;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newDownPartyOAuthResourceForm.SetFieldError(nameof(newDownPartyOAuthResourceForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task OnNewDownPartySamlModalValidSubmitAsync(NewDownPartyViewModel newDownPartyViewModel, PageEditForm<NewDownPartySamlViewModel> newDownPartySamlForm, EditContext editContext)
        {
            try
            {
                // first map to add default ViewModel values
                var SamlDownPartyViewModel = newDownPartySamlForm.Model.Map<SamlDownPartyViewModel>();
                var samlDownParty = SamlDownPartyViewModel.Map<SamlDownParty>(afterMap: afterMap =>
                {
                    afterMap.AllowUpPartyNames = new List<string> { Constants.DefaultLogin.Name };
                    afterMap.Claims = new List<string> { ClaimTypes.Email, ClaimTypes.Name, ClaimTypes.GivenName, ClaimTypes.Surname };
                });

                var samlDownPartyResult = await DownPartyService.CreateSamlDownPartyAsync(samlDownParty);
                toastService.ShowSuccess("SAML 2.0 authentication method created.");

                newDownPartySamlForm.Model.Name = samlDownPartyResult.Name;
                newDownPartySamlForm.Model.Issuer = samlDownPartyResult.Issuer;
                newDownPartySamlForm.Model.DisplayName = samlDownPartyResult.DisplayName;
                newDownPartySamlForm.Model.Metadata = MetadataLogic.GetDownSamlMetadata(newDownPartySamlForm.Model.Name);                
                downParties.Add(new GeneralSamlDownPartyViewModel(new DownParty { Type = PartyTypes.Saml2, Name = newDownPartySamlForm.Model.Name, DisplayName = newDownPartySamlForm.Model.DisplayName }));
                newDownPartyViewModel.Created = true;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newDownPartySamlForm.SetFieldError(nameof(newDownPartySamlForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
