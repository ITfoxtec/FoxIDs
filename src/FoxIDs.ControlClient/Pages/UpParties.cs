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
using FoxIDs.Util;

namespace FoxIDs.Client.Pages
{
    public partial class UpParties
    {
        private NewUpPartyViewModel newUpPartyModal;
        private DownPartyTestViewModel testDownPartyModal;
        private PageEditForm<FilterUpPartyViewModel> upPartyFilterForm;
        private List<GeneralUpPartyViewModel> upParties;
        private string paginationToken;

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
            upPartyFilterForm?.ClearError();
            if (newUpPartyModal != null)
            {
                newUpPartyModal.IsVisible = false;
            }
            try
            {
                SetGeneralUpParties(await UpPartyService.GetUpPartiesAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                upParties?.Clear();
                upPartyFilterForm.SetError(ex.Message);
            }
        }
        
        private async Task OnUpPartyFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralUpParties(await UpPartyService.GetUpPartiesAsync(upPartyFilterForm.Model.FilterName));
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

        private async Task LoadMorePartiesAsync()
        {
            try
            {
                SetGeneralUpParties(await UpPartyService.GetUpPartiesAsync(upPartyFilterForm.Model.FilterName, paginationToken: paginationToken), addParties: true);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
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

        private void SetGeneralUpParties(PaginationResponse<UpParty> dataUpParties, bool addParties = false)
        {
            var ups = new List<GeneralUpPartyViewModel>();
            foreach (var dp in dataUpParties.Data)
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

            if (upParties != null && addParties)
            {
                upParties.AddRange(ups);
            }
            else
            {
                upParties = ups;
            }
            paginationToken = dataUpParties.PaginationToken;
        }

        private GeneralUpPartyViewModel ShowCreateUpParty(PartyTypes type, bool tokenExchange = false, UpPartyModuleTypes? moduleType = null)
        {
            GeneralUpPartyViewModel newUpParty = null;
            if (type == PartyTypes.Login)
            {
                var loginUpParty = new GeneralLoginUpPartyViewModel();
                loginUpParty.CreateMode = true;
                loginUpParty.Edit = true;
                newUpParty = loginUpParty;
            }
            else if (type == PartyTypes.OAuth2)
            {
                var oauthUpParty = new GeneralOAuthUpPartyViewModel();
                oauthUpParty.CreateMode = true;
                oauthUpParty.Edit = true;
                oauthUpParty.TokenExchange = tokenExchange;
                newUpParty = oauthUpParty;
            }
            else if (type == PartyTypes.Oidc)
            {
                var oidcUpParty = new GeneralOidcUpPartyViewModel();
                oidcUpParty.CreateMode = true;
                oidcUpParty.Edit = true;
                oidcUpParty.TokenExchange = tokenExchange;
                newUpParty = oidcUpParty;
            }
            else if (type == PartyTypes.Saml2)
            {
                var samlUpParty = new GeneralSamlUpPartyViewModel();
                samlUpParty.CreateMode = true;
                samlUpParty.Edit = true;
                samlUpParty.TokenExchange = tokenExchange;
                samlUpParty.ModuleType = moduleType;
                newUpParty = samlUpParty;
            }
            else if (type == PartyTypes.TrackLink)
            {
                var trackLinkUpParty = new GeneralTrackLinkUpPartyViewModel();
                trackLinkUpParty.CreateMode = true;
                trackLinkUpParty.Edit = true;
                newUpParty = trackLinkUpParty;
            }
            else if (type == PartyTypes.ExternalLogin)
            {
                var extLoginUpParty = new GeneralExternalLoginUpPartyViewModel();
                extLoginUpParty.CreateMode = true;
                extLoginUpParty.Edit = true;
                newUpParty = extLoginUpParty;
            }

            return newUpParty;
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

        private string GetUpPartyDisplayName(UpParty upParty)
        {
            var displayName = upParty.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                if (upParty.Type == PartyTypes.Login && upParty.Name == Constants.DefaultLogin.Name)
                {
                    displayName = "Default";
                }
                else
                {
                    displayName = upParty.Name;
                }
            }

            return displayName;
        }

        private string GetUpPartyTypeLabel(UpParty upParty)
        {
            if (upParty.Type == PartyTypes.Saml2 && upParty.ModuleType == UpPartyModuleTypes.NemLogin)
            {
                return "SAML 2.0 - NemLog-in";
            }

            return upParty.Type switch
            {
                PartyTypes.Login => "User Login UI",
                PartyTypes.OAuth2 => "OAuth 2.0",
                PartyTypes.Oidc => "OpenID Connect",
                PartyTypes.Saml2 => "SAML 2.0",
                PartyTypes.TrackLink => "Environment Link",
                PartyTypes.ExternalLogin => "External API Login",
                _ => upParty.Type.ToString()
            };
        }

        private void ShowNewUpParty()
        {
            newUpPartyModal.Init();
            newUpPartyModal.IsVisible = true;
            StateHasChanged();
        }

        private void HideNewUpParty()
        {
            newUpPartyModal.Init();
            StateHasChanged();
        }

        private async Task ChangeNewUpPartyStateAsync(string appTitle = null, PartyTypes? type = null, bool tokenExchange = false, UpPartyModuleTypes? moduleType = null)
        {
            if (!type.HasValue)
            {
                newUpPartyModal.AppTitle = null;
                newUpPartyModal.Type = null;
                newUpPartyModal.ShowAdvanced = false;
                newUpPartyModal.CreateWorking = false;
                newUpPartyModal.Created = false;
                newUpPartyModal.SelectTracks = null;
                newUpPartyModal.SelectTrackFilterForm = new PageEditForm<FilterTrackViewModel>();
                newUpPartyModal.EnvironmentLinkForm = new PageEditForm<NewUpPartyEnvironmentLinkViewModel>();
                return;
            }

            if(type == PartyTypes.TrackLink)
            {
                await LoadTracksAsync();
                newUpPartyModal.AppTitle = appTitle;
                newUpPartyModal.Type = type;
            }
            else if(type.HasValue)
            {
                var newUpParty = ShowCreateUpParty(type.Value, tokenExchange, moduleType);
                if (newUpParty != null)
                {
                    upParties.Insert(0, newUpParty);
                }
                HideNewUpParty();
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
                    RedirectUri = $"{RouteBindingLogic.GetBaseUri().Trim('/')}/{TenantName}/applications/test#id={RandomName.GenerateDefaultName()}".ToLower()
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
                var selectTrackTasks = (await TrackService.GetTracksAsync(filterName)).Data.OrderTracks();
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
                    AllowUpParties = new List<UpPartyLink>
                    {
                        new UpPartyLink { Name = Constants.DefaultLogin.Name }
                    },
                    Claims = new List<OAuthDownClaim>
                    {
                        new OAuthDownClaim { Claim = "*" }
                    }
                }, trackName: newUpPartyOAuthEnvironmentLinkForm.Model.ToDownTrackName);

                trackLinkUpPartyResult.ToDownPartyName = trackLinkDownPartyResult.Name;
                _ = await UpPartyService.UpdateTrackLinkUpPartyAsync(trackLinkUpPartyResult);
                toastService.ShowSuccess("Environment Link authentication method created.");

                var generalUpPartyViewModel = new GeneralTrackLinkUpPartyViewModel(new UpParty { Type = PartyTypes.TrackLink, Name = trackLinkUpPartyResult.Name, DisplayName = trackLinkUpPartyResult.DisplayName });
                upParties.Insert(0, generalUpPartyViewModel);
                if (upParties.Count() <= 1)
                {
                    ShowUpdateUpParty(generalUpPartyViewModel);
                }
                newDownPartyViewModel.Created = true;
                HideNewUpParty();
            }
            catch (FoxIDsApiException)
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

