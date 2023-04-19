using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using ITfoxtec.Identity;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models;
using System;
using FoxIDs.Models.Api;
using Blazored.Toast.Services;

namespace FoxIDs.Client.Pages.Components
{
    public class UpPartyBase : ComponentBase
    {
        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public TrackSelectedLogic TrackSelectedLogic { get; set; }

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Parameter]
        public EventCallback<GeneralUpPartyViewModel> OnStateHasChanged { get; set; }

        [Parameter]
        public List<GeneralUpPartyViewModel> UpParties { get; set; }

        [Parameter]
        public GeneralUpPartyViewModel UpParty { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        public void ShowLoginTab(GeneralLoginUpPartyViewModel upParty, LoginTabTypes samlTabTypes)
        {
            switch (samlTabTypes)
            {
                case LoginTabTypes.Login:
                    upParty.ShowLoginTab = true;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowCreateUserTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case LoginTabTypes.ClaimsTransform:
                    upParty.ShowLoginTab = false;
                    upParty.ShowClaimTransformTab = true;
                    upParty.ShowCreateUserTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case LoginTabTypes.CreateUser:
                    upParty.ShowLoginTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowCreateUserTab = true;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case LoginTabTypes.Session:
                    upParty.ShowLoginTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowCreateUserTab = false;
                    upParty.ShowSessionTab = true;
                    upParty.ShowHrdTab = false;
                    break;
                case LoginTabTypes.Hrd:
                    upParty.ShowLoginTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowCreateUserTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowOAuthTab(IGeneralOAuthUpPartyTabViewModel upParty, OAuthTabTypes oauthTabTypes)
        {
            switch (oauthTabTypes)
            {
                case OAuthTabTypes.Client:
                    upParty.ShowClientTab = true;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case OAuthTabTypes.ClaimsTransform:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = true;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case OAuthTabTypes.Session:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = true;
                    upParty.ShowHrdTab = false;
                    break;
                case OAuthTabTypes.Hrd:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowSamlTab(GeneralSamlUpPartyViewModel upParty, SamlTabTypes samlTabTypes)
        {
            switch (samlTabTypes)
            {
                case SamlTabTypes.Saml:
                    upParty.ShowSamlTab = true;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case SamlTabTypes.ClaimsTransform:
                    upParty.ShowSamlTab = false;
                    upParty.ShowClaimTransformTab = true;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case SamlTabTypes.Session:
                    upParty.ShowSamlTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = true;
                    upParty.ShowHrdTab = false;
                    break;
                case SamlTabTypes.Hrd:
                    upParty.ShowSamlTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowTrackLinkTab(GeneralTrackLinkUpPartyViewModel upParty, TrackLinkTabTypes trackLinkTabTypes)
        {
            switch (trackLinkTabTypes)
            {
                case TrackLinkTabTypes.TrackLink:
                    upParty.ShowTrackLinkTab = true;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case TrackLinkTabTypes.ClaimsTransform:
                    upParty.ShowTrackLinkTab = false;
                    upParty.ShowClaimTransformTab = true;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = false;
                    break;
                case TrackLinkTabTypes.Session:
                    upParty.ShowTrackLinkTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = true;
                    upParty.ShowHrdTab = false;
                    break;
                case TrackLinkTabTypes.Hrd:
                    upParty.ShowTrackLinkTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowSessionTab = false;
                    upParty.ShowHrdTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public (string, string, string) GetRedirectAndLogoutUrls(string partyName, PartyBindingPatterns partyBindingPattern)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--up-party-name--" : partyName.ToLower()).ToUpPartyBinding(partyBindingPattern);
            var oauthUrl = $"{RouteBindingLogic.GetFoxIDsTenantEndpoint()}/{(RouteBindingLogic.IsMasterTenant ? "master" : TrackSelectedLogic.Track.Name)}/{partyBinding}/{Constants.Routes.OAuthController}/";
            return (oauthUrl + Constants.Endpoints.AuthorizationResponse, oauthUrl + Constants.Endpoints.EndSessionResponse, oauthUrl + Constants.Endpoints.FrontChannelLogout);
        }

        public string GetSamlMetadata(string partyName, PartyBindingPatterns partyBindingPattern)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--up-party-name--" : partyName.ToLower()).ToUpPartyBinding(partyBindingPattern);
            return $"{RouteBindingLogic.GetFoxIDsTenantEndpoint()}/{(RouteBindingLogic.IsMasterTenant ? "master" : TrackSelectedLogic.Track.Name)}/{partyBinding}/{Constants.Routes.SamlController}/{Constants.Endpoints.SamlSPMetadata}";
        }

        public async Task UpPartyCancelAsync(GeneralUpPartyViewModel upParty)
        {
            if (upParty.CreateMode)
            {
                UpParties.Remove(upParty);
            }
            else
            {
                UpParty.Edit = false;
            }
            await OnStateHasChanged.InvokeAsync(UpParty);
        }
    }
}
