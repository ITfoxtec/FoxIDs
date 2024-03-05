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

namespace FoxIDs.Client.Pages
{
    public partial class UpParties
    {
        private NewUpPartyViewModel newUpPartyModal;
        private PageEditForm<FilterUpPartyViewModel> upPartyFilterForm;
        private List<GeneralUpPartyViewModel> upParties;
     
        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            newUpPartyModal = new NewUpPartyViewModel();
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

        private void ShowCreateUpParty(PartyTypes type)
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
                upParties.Add(oauthUpParty);
            }
            else if (type == PartyTypes.Oidc)
            {
                var oidcUpParty = new GeneralOidcUpPartyViewModel();
                oidcUpParty.CreateMode = true;
                oidcUpParty.Edit = true;
                upParties.Add(oidcUpParty);
            }
            else if (type == PartyTypes.Saml2)
            {
                var samlUpParty = new GeneralSamlUpPartyViewModel();
                samlUpParty.CreateMode = true;
                samlUpParty.Edit = true;
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

        private string UpPartyInfoText(GeneralUpPartyViewModel upParty)
        {
            if (upParty.Type == PartyTypes.Login)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (Login)";
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
            throw new NotSupportedException();
        }

        private void ShowNewUpParty()
        {
            newUpPartyModal.Init();
            newUpPartyModal.Modal.Show();
        }

        private async Task ChangeNewUpPartyStateAsync(string appTitle = null, PartyTypes? type = null)
        {
            if(type == PartyTypes.TrackLink)
            {
                await LoadTracksAsync();
                newUpPartyModal.AppTitle = appTitle;
                newUpPartyModal.Type = type;
            }
            else if(type.HasValue)
            {
                ShowCreateUpParty(type.Value);
                newUpPartyModal.Modal.Hide();
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
                newUpPartyModal.SelectTrackTasks = selectTrackTasks.Where(t => t.Name != TrackSelectedLogic.Track.Name);
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

        private void OnNewUpPartyOAuthEnvironmentLinkModalValidAfterInit()
        {
            var track = newUpPartyModal.SelectTrackTasks.FirstOrDefault();
            if (track != null)
            {
                SelectTrack(track);
            }
        }

        private async Task OnNewUpPartyOAuthEnvironmentLinkModalValidSubmitAsync(NewUpPartyViewModel newDownPartyViewModel, PageEditForm<NewUpPartyEnvironmentLinkViewModel> newUpPartyOAuthEnvironmentLinkForm, EditContext editContext)
        {
            try
            {
                newDownPartyViewModel.CreateWorking = true;


                //newDownPartyOAuthClientForm.Model.Secret = SecretGenerator.GenerateNewSecret();

                //var oauthDownParty = newDownPartyOAuthClientForm.Model.Map<OAuthDownParty>(afterMap: afterMap =>
                //{
                //    afterMap.AllowUpPartyNames = new List<string> { Constants.DefaultLogin.Name };

                //    afterMap.Client = new OAuthDownClient
                //    {
                //        RequirePkce = false,
                //    };
                //});

                //var oauthDownPartyResult = await DownPartyService.CreateOAuthDownPartyAsync(oauthDownParty);
                //await DownPartyService.CreateOAuthClientSecretDownPartyAsync(new OAuthClientSecretRequest { PartyName = oauthDownPartyResult.Name, Secrets = new List<string> { newDownPartyOAuthClientForm.Model.Secret } });
                //toastService.ShowSuccess("OAuth 2.0 authentication method created.");

                //newDownPartyOAuthClientForm.Model.Name = oauthDownPartyResult.Name;
                //newDownPartyOAuthClientForm.Model.DisplayName = oauthDownPartyResult.DisplayName;
                //(var clientAuthority, _) = MetadataLogic.GetDownAuthorityAndOIDCDiscovery(newDownPartyOAuthClientForm.Model.Name, false);
                //newDownPartyOAuthClientForm.Model.Authority = clientAuthority;
                //downParties.Add(new GeneralOAuthDownPartyViewModel(new DownParty { Type = PartyTypes.OAuth2, Name = newDownPartyOAuthClientForm.Model.Name, DisplayName = newDownPartyOAuthClientForm.Model.DisplayName }));
                newDownPartyViewModel.Created = true;
            }
            catch (FoxIDsApiException ex)
            {
                newDownPartyViewModel.CreateWorking = false;
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    newUpPartyOAuthEnvironmentLinkForm.SetFieldError(nameof(newUpPartyOAuthEnvironmentLinkForm.Model.Name), ex.Message);
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
    }
}
