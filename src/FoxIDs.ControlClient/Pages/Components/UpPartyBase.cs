using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using ITfoxtec.Identity;
using FoxIDs.Client.Models.Config;

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
