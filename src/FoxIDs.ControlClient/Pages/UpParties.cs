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
using System.Linq;
using Blazored.Toast.Services;

namespace FoxIDs.Client.Pages
{
    public partial class UpParties
    {
        private NewUpPartyViewModel newUpPartyModal;
        private DownPartyTestViewModel testDownPartyModal;
        private PageEditForm<FilterUpPartyViewModel> upPartyFilterForm;
        private List<GeneralUpPartyViewModel> upParties;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Inject]
        public MetadataLogic MetadataLogic { get; set; }

        [Inject]
        public WizardService WizardService { get; set; }

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
            newUpPartyModal = new NewUpPartyViewModel();
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
            var ups = new List<GeneralUpPartyViewModel>();
            foreach (var dp in dataUpParties)
            {
                if (dp.Type == PartyTypes.Login)
                {
                    ups.Add(new GeneralLoginUpPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.OAuth2)
                {
                    ups.Add(new GeneralOAuthUpPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.Oidc)
                {
                    ups.Add(new GeneralOidcUpPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.Saml2)
                {
                    ups.Add(new GeneralSamlUpPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.TrackLink)
                {
                    ups.Add(new GeneralTrackLinkUpPartyViewModel(dp));
                }
                else if (dp.Type == PartyTypes.ExternalLogin)
                {
                    ups.Add(new GeneralExternalLoginUpPartyViewModel(dp));
                }
            }
            upParties = ups;
        }

        private void ShowCreateUpParty(PartyTypes type, bool tokenExchange = false)
        {
            if (type == PartyTypes.Login)
            {
                var loginUpParty = new GeneralLoginUpPartyViewModel();
                loginUpParty.CreateMode = true;
                loginUpParty.Edit = true;
                upParties.Add(loginUpParty);
            }
            else if (type == PartyTypes.OAuth2)
            {
                var oauthUpParty = new GeneralOAuthUpPartyViewModel();
                oauthUpParty.CreateMode = true;
                oauthUpParty.Edit = true;
                oauthUpParty.TokenExchange = tokenExchange;
                upParties.Add(oauthUpParty);
            }
            else if (type == PartyTypes.Oidc)
            {
                var oidcUpParty = new GeneralOidcUpPartyViewModel();
                oidcUpParty.CreateMode = true;
                oidcUpParty.Edit = true;
                oidcUpParty.TokenExchange = tokenExchange;
                upParties.Add(oidcUpParty);
            }
            else if (type == PartyTypes.Saml2)
            {
                var samlUpParty = new GeneralSamlUpPartyViewModel();
                samlUpParty.CreateMode = true;
                samlUpParty.Edit = true;
                samlUpParty.TokenExchange = tokenExchange;
                upParties.Add(samlUpParty);
            }
            else if (type == PartyTypes.TrackLink)
            {
                var trackLinkUpParty = new GeneralTrackLinkUpPartyViewModel();
                trackLinkUpParty.CreateMode = true;
                trackLinkUpParty.Edit = true;
                upParties.Add(trackLinkUpParty);
            }
            else if (type == PartyTypes.ExternalLogin)
            {
                var extLoginUpParty = new GeneralExternalLoginUpPartyViewModel();
                extLoginUpParty.CreateMode = true;
                extLoginUpParty.Edit = true;
                upParties.Add(extLoginUpParty);
            }
        }

        private void ShowUpdateUpParty(GeneralUpPartyViewModel upParty)
        {
            upParty.CreateMode = false;
            upParty.DeleteAcknowledge = false;
            upParty.ShowAdvanced = false;
            upParty.Error = null;
            upParty.Edit = true;          
        }

        private async Task OnStateHasChangedAsync(GeneralUpPartyViewModel upParty)
        {
            await InvokeAsync(StateHasChanged);
        }

        private string UpPartyInfoText(UpParty upParty)
        {
            if (upParty.Type == PartyTypes.Login)
            {
                return $"{upParty.DisplayName ?? (upParty.Name == Constants.DefaultLogin.Name ? "Default" : upParty.Name)} (User Login UI)";
            }
            else if (upParty.Type == PartyTypes.OAuth2)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (OAuth 2.0)";
            }
            else if (upParty.Type == PartyTypes.Oidc)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (OpenID Connect)";
            }
            else if (upParty.Type == PartyTypes.Saml2)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (SAML 2.0)";
            }
            else if (upParty.Type == PartyTypes.TrackLink)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (Environment Link)";
            }
            else if (upParty.Type == PartyTypes.ExternalLogin)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (External API Login)";
            }
            throw new NotSupportedException($"Type '{upParty.Type}'.");
        }

        private void ShowNewUpParty()
        {
            newUpPartyModal.Init();
            newUpPartyModal.Modal.Show();
        }

        private async Task ChangeNewUpPartyStateAsync()
        {
            await ChangeNewUpPartyStateAsync(string.Empty);
        }

        private async Task ChangeNewUpPartyStateAsync(string appTitle = null, PartyTypes? type = null, bool tokenExchange = false)
        {
            await ChangeNewUpPartyStateAsync(new UpPartyInfo { Title = appTitle }, type, tokenExchange: tokenExchange);
        }

        private async Task ChangeNewUpPartyStateAsync(UpPartyInfo appInfo = null, PartyTypes? type = null, IdPTypes? idpType = null, bool tokenExchange = false)
        {
            if(type == PartyTypes.TrackLink || idpType.HasValue)
            {
                if (type == PartyTypes.TrackLink)
                {
                    await LoadTracksAsync();
                }
                newUpPartyModal.AppTitle = appInfo?.Title;
                newUpPartyModal.AppSubTitle = appInfo?.SubTitle;
                newUpPartyModal.AppTitleImage = appInfo?.Image;
                newUpPartyModal.AppTitleImageHeight = appInfo?.ImageHeight;
                newUpPartyModal.Type = type;
                newUpPartyModal.IdPType = idpType;
                StateHasChanged();
            }
            else if(type.HasValue)
            {
                ShowCreateUpParty(type.Value, tokenExchange);
                newUpPartyModal.Modal.Hide();
            }
            else
            {
                newUpPartyModal.Type = null;
                newUpPartyModal.IdPType = null;
                newUpPartyModal.ShowAdvanced = false;
            }
        }

        private async Task InitAndShowTestUpPartyAsync(GeneralUpPartyViewModel upParty)
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
                    UpParties = GetUpParties(upParty),
                    RedirectUri = $"{RouteBindingLogic.GetBaseUri().Trim('/')}/{TenantName}/applications/test".ToLower()
                });

                testDownPartyModal.DisplayName = downPartyTestStartResponse.DisplayName;
                testDownPartyModal.TestUrl = downPartyTestStartResponse.TestUrl;
                testDownPartyModal.TestExpireAt = downPartyTestStartResponse.TestExpireAt;

                StateHasChanged();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                testDownPartyModal.Error = ex.Message;
            }
        }

        private List<UpPartyLink> GetUpParties(UpParty up)
        {
            var upParties = new List<UpPartyLink>
            {
                new UpPartyLink { Name = up.Name }
            };
            if (up.Profiles != null)
            {
                foreach (var profile in up.Profiles)
                {
                    upParties.Add(new UpPartyLink { Name = up.Name, ProfileName = profile.Name });
                }
            }
            return upParties;
        }

        private void ShowSelectTrack(NewUpPartyEnvironmentLinkViewModel newUpPartyEnvironmentLinkViewModel)
        {
            newUpPartyEnvironmentLinkViewModel.ToDownTrackName = null;
            newUpPartyEnvironmentLinkViewModel.ToDownTrackDisplayName = null;
        }

        private async Task LoadTracksAsync(string filterName = null)
        {
            try
            {
                var selectTrackTasks = (await TrackService.FilterTrackAsync(filterName)).OrderTracks();
                newUpPartyModal.SelectTracks = selectTrackTasks.Where(t => t.Name != TrackSelectedLogic.Track.Name);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    newUpPartyModal.SelectTrackFilterForm.SetFieldError(nameof(newUpPartyModal.SelectTrackFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private Task OnSelectTrackFilterValidSubmitAsync(EditContext editContext)
        {
            return LoadTracksAsync(newUpPartyModal.SelectTrackFilterForm.Model.FilterName);
        }

        private void SelectTrack(Track track)
        {
            newUpPartyModal.EnvironmentLinkForm.Model.ToDownTrackName = track.Name;
            newUpPartyModal.EnvironmentLinkForm.Model.ToDownTrackDisplayName = track.DisplayName.GetConcatProdTrackName();
        }

        private async Task OnNewUpPartyEnvironmentLinkModalValidSubmitAsync(NewUpPartyViewModel newUpPartyViewModel, PageEditForm<NewUpPartyEnvironmentLinkViewModel> newUpPartyEnvironmentLinkForm, EditContext editContext)
        {
            try
            {
                newUpPartyViewModel.CreateWorking = true;

                var trackLinkUpParty = newUpPartyEnvironmentLinkForm.Model.Map<TrackLinkUpParty>(afterMap: afterMap =>
                {
                    afterMap.ToDownPartyName = "x";
                    afterMap.SelectedUpParties = new List<string> { "*" };
                    afterMap.Claims = new List<string> { "*" };
                    afterMap.PipeExternalId = true;
                });
                var trackLinkUpPartyResult = await UpPartyService.CreateTrackLinkUpPartyAsync(trackLinkUpParty);

                var trackLinkDownPartyResult = await DownPartyService.CreateTrackLinkDownPartyAsync(new TrackLinkDownParty
                {
                    DisplayName = newUpPartyEnvironmentLinkForm.Model.DisplayName,
                    ToUpTrackName = TrackSelectedLogic.Track.Name,
                    ToUpPartyName = trackLinkUpPartyResult.Name,
                    AllowUpPartyNames = new List<string> { Constants.DefaultLogin.Name },
                    Claims = new List<OAuthDownClaim>
                    {
                        new OAuthDownClaim { Claim = "*" }
                    }
                }, trackName: newUpPartyEnvironmentLinkForm.Model.ToDownTrackName);

                trackLinkUpPartyResult.ToDownPartyName = trackLinkDownPartyResult.Name;
                _ = await UpPartyService.UpdateTrackLinkUpPartyAsync(trackLinkUpPartyResult);
                toastService.ShowSuccess("Environment Link authentication method created.");

                var generalUpPartyViewModel = new GeneralTrackLinkUpPartyViewModel(new UpParty { Type = PartyTypes.TrackLink, Name = trackLinkUpPartyResult.Name, DisplayName = trackLinkUpPartyResult.DisplayName });
                upParties.Add(generalUpPartyViewModel);
                if (upParties.Count() <= 1)
                {
                    ShowUpdateUpParty(generalUpPartyViewModel);
                }
                newUpPartyViewModel.Created = true;
            }
            catch (FoxIDsApiException ex)
            {
                newUpPartyViewModel.CreateWorking = false;
                throw;
            }
            catch
            {
                newUpPartyViewModel.CreateWorking = false;
            }
        }

        private async Task OnNewUpPartyNemLoginModalValidSubmitAsync(NewUpPartyViewModel newUpPartyViewModel, PageEditForm<NewUpPartyNemLoginViewModel> newUpPartyNemLoginForm, EditContext editContext)
        {
            try
            {
                newUpPartyViewModel.CreateWorking = true;


                var samlUpParty = new SamlUpParty
                {
                    PartyBindingPattern = PartyBindingPatterns.Dot,
                    DisplayName = newUpPartyNemLoginForm.Model.DisplayName,
                    Name = await UpPartyService.GetNewPartyNameAsync(),
                    AuthnRequestBinding = SamlBindingTypes.Post,
                    AuthnResponseBinding = SamlBindingTypes.Post,
                    LogoutRequestBinding = SamlBindingTypes.Post,
                    LogoutResponseBinding = SamlBindingTypes.Post,
                    SignAuthnRequest = true,
                    DisableLoginHint = true,                    
                    MetadataIncludeEncryptionCertificates = true,
                    MetadataNameIdFormats = ["urn:oasis:names:tc:SAML:2.0:nameid-format:persistent"],
                    Claims = newUpPartyNemLoginForm.Model.Claims.Where(c => newUpPartyNemLoginForm.Model.SelectedClaims.Where(s => s.Equals(c, StringComparison.Ordinal)).Any() == true).ToList()
                };

                samlUpParty.SpIssuer = $"{RouteBindingLogic.GetFoxIDsTenantEndpoint().Replace("://", (newUpPartyNemLoginForm.Model.NemLoginEnvironment == NemLoginEnvironments.Test ? "://samltest." : "://saml."))}/{TrackSelectedLogic.Track.Name}/{samlUpParty.Name}/";

                samlUpParty.MetadataAttributeConsumingServices = [new SamlMetadataAttributeConsumingService
                {
                    ServiceName = new SamlMetadataServiceName { Name = samlUpParty.DisplayName, Lang = "en" },
                    RequestedAttributes = new List<SamlMetadataRequestedAttribute>()
                }];
                
                foreach(var claim in samlUpParty.Claims)
                {
                    samlUpParty.MetadataAttributeConsumingServices[0].RequestedAttributes.Add(new SamlMetadataRequestedAttribute
                    {
                        Name = claim,
                        IsRequired = claim.Equals("https://data.gov.dk/concept/core/nsis/loa", StringComparison.Ordinal) ? true: false,
                        NameFormat = "urn:oasis:names:tc:SAML:2.0:attrname-format:uri"
                    });
                }

                var privilegesClaim = "https://data.gov.dk/model/core/eid/privilegesIntermediate";
                if (samlUpParty.Claims.Where(c => c.Equals(privilegesClaim, StringComparison.Ordinal)).Any())
                {
                    samlUpParty.ClaimTransforms = [new SamlClaimTransform 
                    {
                        Type = ClaimTransformTypes.DkPrivilege,
                        Order = 1,
                        Action = ClaimTransformActions.Replace,
                        ClaimsIn = [privilegesClaim],
                        ClaimOut = privilegesClaim,
                    }];
                }

                if (newUpPartyNemLoginForm.Model.NemLoginEnvironment == NemLoginEnvironments.Production)
                {
                    samlUpParty.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.Online;

                    samlUpParty.MetadataContactPersons = [new SamlMetadataContactPerson
                    {
                        ContactType = SamlMetadataContactPersonTypes.Technical,
                        Company = newUpPartyNemLoginForm.Model.Company,
                        //GivenName = newUpPartyNemLoginForm.Model.GivenName,
                        //Surname = newUpPartyNemLoginForm.Model.Surname,
                        EmailAddress = newUpPartyNemLoginForm.Model.EmailAddress,
                        //TelephoneNumber = newUpPartyNemLoginForm.Model.TelephoneNumber,
                    }];
                }

                newUpPartyNemLoginForm.Model.Metadata = MetadataLogic.GetUpSamlMetadata(samlUpParty.Name, samlUpParty.PartyBindingPattern);

                var wizardNemLoginSettings = await WizardService.ReadNemLoginSettingsAsync();
                if (newUpPartyNemLoginForm.Model.NemLoginEnvironment == NemLoginEnvironments.Test)
                {
                    samlUpParty.MetadataUrl = wizardNemLoginSettings.OioSaml3MetadataTest;
                    
                    var trackKey = await TrackService.GetTrackKeyTypeAsync();
                    if (trackKey.Type == TrackKeyTypes.Contained)
                    {
                        var trackKeys = await TrackService.GetTrackKeyContainedAsync();
                        if (trackKeys.SecondaryKey != null)
                        {
                            await TrackService.DeleteTrackKeyContainedAsync();
                        }
                    }
                    else
                    {
                        await TrackService.UpdateTrackKeyTypeAsync(new TrackKey { Type = TrackKeyTypes.Contained });
                    }
                    _ = await TrackService.UpdateTrackKeyContainedAsync(new TrackKeyItemContainedRequest 
                    {
                        IsPrimary = true,
                        Key = wizardNemLoginSettings.Oces3TestCertificate
                    });
                }
                else
                {
                    samlUpParty.MetadataUrl = wizardNemLoginSettings.OioSaml3MetadataProduction;
                }



                _ = await UpPartyService.CreateSamlUpPartyAsync(samlUpParty);
                toastService.ShowSuccess("NemLog-in (SAML 2.0) authentication method created.");

                var generalUpPartyViewModel = new GeneralSamlUpPartyViewModel(new UpParty { Type = PartyTypes.Saml2, Name = samlUpParty.Name, DisplayName = samlUpParty.DisplayName });
                upParties.Add(generalUpPartyViewModel);
                if (upParties.Count() <= 1)
                {
                    ShowUpdateUpParty(generalUpPartyViewModel);
                }
                newUpPartyViewModel.Created = true;
            }
            catch (FoxIDsApiException ex)
            {
                newUpPartyViewModel.CreateWorking = false;
                throw;
            }
            catch
            {
                newUpPartyViewModel.CreateWorking = false;
            }
        }
    }
}
