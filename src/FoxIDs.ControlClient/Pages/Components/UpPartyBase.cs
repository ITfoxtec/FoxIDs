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

namespace FoxIDs.Client.Pages.Components
{
    public class UpPartyBase : ComponentBase
    {
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

        public void ShowLoginTab(GeneralLoginUpPartyViewModel downParty, LoginTabTypes samlTabTypes)
        {
            switch (samlTabTypes)
            {
                case LoginTabTypes.Login:
                    downParty.ShowLoginTab = true;
                    downParty.ShowClaimTransformTab = false;
                    break;
                case LoginTabTypes.ClaimsTransform:
                    downParty.ShowLoginTab = false;
                    downParty.ShowClaimTransformTab = true;
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
                    break;
                case OAuthTabTypes.ClaimsTransform:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowSamlTab(GeneralSamlUpPartyViewModel downParty, SamlTabTypes samlTabTypes)
        {
            switch (samlTabTypes)
            {
                case SamlTabTypes.Saml:
                    downParty.ShowSamlTab = true;
                    downParty.ShowClaimTransformTab = false;
                    break;
                case SamlTabTypes.ClaimsTransform:
                    downParty.ShowSamlTab = false;
                    downParty.ShowClaimTransformTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public (string, string) GetAuthorityAndOIDCDiscovery(string partyName)
        {
            var authority = $"{ClientSettings.FoxIDsEndpoint}/{TenantName}/{(RouteBindingLogic.IsMasterTenant ? "master" : TrackSelectedLogic.Track.Name)}/({(partyName.IsNullOrEmpty() ? "?" : partyName.ToLower())})/";
            return (authority, new Uri(new Uri(authority), IdentityConstants.OidcDiscovery.Path).OriginalString);
        }

        public string GetSamlMetadata(string partyName)
        {
            return $"{ClientSettings.FoxIDsEndpoint}/{TenantName}/{(RouteBindingLogic.IsMasterTenant ? "master" : TrackSelectedLogic.Track.Name)}/({(partyName.IsNullOrEmpty() ? "?" : partyName.ToLower())})/saml/spmetadata";
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
