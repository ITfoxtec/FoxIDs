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
using FoxIDs.Client.Models;

namespace FoxIDs.Client.Pages
{
    public partial class DownParties 
    {
        private PageEditForm<FilterDownPartyViewModel> downPartyFilterForm;
        private List<GeneralDownPartyViewModel> downParties = new List<GeneralDownPartyViewModel>();

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public DownPartyService DownPartyService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
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

        private void ShowCreateDownParty(PartyTypes type, OAuthSubPartyTypes? oauthSubPartyType = null)
        {
            if (type == PartyTypes.Oidc)
            {
                var oidcDownParty = new GeneralOidcDownPartyViewModel();
                oidcDownParty.CreateMode = true;
                oidcDownParty.Edit = true;
                downParties.Add(oidcDownParty);
            }
            else if (type == PartyTypes.OAuth2)
            {
                var oauthDownParty = new GeneralOAuthDownPartyViewModel();
                if (!oauthSubPartyType.HasValue)
                {
                    throw new ArgumentNullException(nameof(oauthSubPartyType), "Required for OAuth 2.0 down parties.");
                }
                oauthDownParty.SubPartyType = oauthSubPartyType.Value;
                oauthDownParty.CreateMode = true;
                oauthDownParty.Edit = true;
                downParties.Add(oauthDownParty);
            }
            else if (type == PartyTypes.Saml2)
            {
                var samlDownParty = new GeneralSamlDownPartyViewModel();
                samlDownParty.CreateMode = true;
                samlDownParty.Edit = true;
                downParties.Add(samlDownParty); 
            }
            else if (type == PartyTypes.TrackLink)
            {
                var trackLinkDownParty = new GeneralTrackLinkDownPartyViewModel();
                trackLinkDownParty.CreateMode = true;
                trackLinkDownParty.Edit = true;
                downParties.Add(trackLinkDownParty); 
            }
        }

        private void ShowUpdateDownParty(GeneralDownPartyViewModel downParty)
        {
            downParty.CreateMode = false;
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
    }
}
