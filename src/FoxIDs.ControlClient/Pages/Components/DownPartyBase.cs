using FoxIDs.Client.Logic;
using FoxIDs.Client.Models;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using ITfoxtec.Identity;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages.Components
{
    public abstract class DownPartyBase : ComponentBase
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
        public DownPartyService DownPartyService { get; set; }

        [Parameter]
        public EventCallback<GeneralDownPartyViewModel> OnStateHasChanged { get; set; }

        [Parameter]
        public List<GeneralDownPartyViewModel> DownParties { get; set; }

        [Parameter]
        public GeneralDownPartyViewModel DownParty { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        public void ShowOAuthTab(IGeneralOAuthDownPartyTabViewModel downParty, OAuthTabTypes oauthTabTypes)
        {
            switch (oauthTabTypes)
            {
                case OAuthTabTypes.Client:
                    downParty.ShowClientTab = true;
                    downParty.ShowResourceTab = false;
                    downParty.ShowClaimTransformTab = false;
                    break;
                case OAuthTabTypes.Resource:
                    downParty.ShowClientTab = false;
                    downParty.ShowResourceTab = true;
                    downParty.ShowClaimTransformTab = false;
                    break;
                case OAuthTabTypes.ClaimsTransform:
                    downParty.ShowClientTab = false;
                    downParty.ShowResourceTab = false;
                    downParty.ShowClaimTransformTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowSamlTab(GeneralSamlDownPartyViewModel downParty, SamlTabTypes samlTabTypes)
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

        public void AddAllowUpPartyName((IAllowUpPartyNames model, string upPartyName) arg)
        {
            if (!arg.model.AllowUpPartyNames.Where(p => p.Equals(arg.upPartyName, StringComparison.OrdinalIgnoreCase)).Any())
            {
                arg.model.AllowUpPartyNames.Add(arg.upPartyName);
            }
        }

        public void RemoveAllowUpPartyName((IAllowUpPartyNames model, string upPartyName) arg)
        {
            arg.model.AllowUpPartyNames.Remove(arg.upPartyName);
        }

        public (string, string) GetAuthorityAndOIDCDiscovery(string partyName)
        {
            var authority = $"{ClientSettings.FoxIDsEndpoint}/{TenantName}/{(RouteBindingLogic.IsMasterTenant ? "master" : TrackSelectedLogic.Track.Name)}/{(partyName.IsNullOrEmpty() ? "?" : partyName.ToLower())}(login)/";
            return (authority, new Uri(new Uri(authority), IdentityConstants.OidcDiscovery.Path).OriginalString);
        }

        public string GetSamlMetadata(string partyName)
        {
            return $"{ClientSettings.FoxIDsEndpoint}/{TenantName}/{(RouteBindingLogic.IsMasterTenant ? "master" : TrackSelectedLogic.Track.Name)}/{(partyName.IsNullOrEmpty() ? "?" : partyName.ToLower())}(login)/saml/idpmetadata";
        }

        public async Task DownPartyCancelAsync(GeneralDownPartyViewModel downParty)
        {
            if (downParty.CreateMode)
            {
                DownParties.Remove(downParty);
            }
            else
            {
                DownParty.Edit = false;
            }
            await OnStateHasChanged.InvokeAsync(DownParty);
        }
    }
}
