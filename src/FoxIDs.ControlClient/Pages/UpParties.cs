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
                return $"{upParty.DisplayName ?? (upParty.Name == Constants.DefaultLogin.Name ? "Default" : upParty.Name)} (User login UI)";
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
            throw new NotSupportedException($"Type '{upParty.Type}'.");
        }

        private void ShowNewUpParty()
        {
            newUpPartyModal.Init();
            newUpPartyModal.Modal.Show();
        }

        private async Task ChangeNewUpPartyStateAsync(string appTitle = null, PartyTypes? type = null, bool tokenExchange = false)
        {
            if(type == PartyTypes.TrackLink)
            {
                await LoadTracksAsync();
                newUpPartyModal.AppTitle = appTitle;
                newUpPartyModal.Type = type;
            }
            else if(type.HasValue)
            {
                ShowCreateUpParty(type.Value, tokenExchange);
                newUpPartyModal.Modal.Hide();
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
                    UpPartyNames = new List<string> { upParty.Name },
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

        private async Task OnNewUpPartyOAuthEnvironmentLinkModalValidSubmitAsync(NewUpPartyViewModel newDownPartyViewModel, PageEditForm<NewUpPartyEnvironmentLinkViewModel> newUpPartyOAuthEnvironmentLinkForm, EditContext editContext)
        {
            try
            {
                newDownPartyViewModel.CreateWorking = true;

                var trackLinkUpParty = newUpPartyOAuthEnvironmentLinkForm.Model.Map<TrackLinkUpParty>(afterMap: afterMap =>
                {
                    afterMap.ToDownPartyName = "x";
                    afterMap.SelectedUpParties = new List<string> { "*" };
                    afterMap.Claims = new List<string> { "*" };
                    afterMap.PipeExternalId = true;
                });
                var trackLinkUpPartyResult = await UpPartyService.CreateTrackLinkUpPartyAsync(trackLinkUpParty);

                var trackLinkDownPartyResult = await DownPartyService.CreateTrackLinkDownPartyAsync(new TrackLinkDownParty
                {
                    DisplayName = newUpPartyOAuthEnvironmentLinkForm.Model.DisplayName,
                    ToUpTrackName = TrackSelectedLogic.Track.Name,
                    ToUpPartyName = trackLinkUpPartyResult.Name,
                    AllowUpPartyNames = new List<string> { Constants.DefaultLogin.Name },
                    Claims = new List<OAuthDownClaim>
                    {
                        new OAuthDownClaim { Claim = "*" }
                    }
                }, trackName: newUpPartyOAuthEnvironmentLinkForm.Model.ToDownTrackName);

                trackLinkUpPartyResult.ToDownPartyName = trackLinkDownPartyResult.Name;
                _ = await UpPartyService.UpdateTrackLinkUpPartyAsync(trackLinkUpPartyResult);
                toastService.ShowSuccess("Environment Link authentication method created.");

                var generalUpPartyViewModel = new GeneralTrackLinkUpPartyViewModel(new UpParty { Type = PartyTypes.TrackLink, Name = trackLinkUpPartyResult.Name, DisplayName = trackLinkUpPartyResult.DisplayName });
                upParties.Add(generalUpPartyViewModel);
                if (upParties.Count() <= 1)
                {
                    ShowUpdateUpParty(generalUpPartyViewModel);
                }
                newDownPartyViewModel.Created = true;
            }
            catch (FoxIDsApiException ex)
            {
                newDownPartyViewModel.CreateWorking = false;
                throw;
            }
            catch
            {
                newDownPartyViewModel.CreateWorking = false;
            }
        }
    }
}
