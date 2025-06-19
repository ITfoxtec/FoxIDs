using Blazored.Toast.Services;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
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
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public TrackSelectedLogic TrackSelectedLogic { get; set; }
    
        [Inject]
        public MetadataLogic MetadataLogic { get; set; }

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public DownPartyService DownPartyService { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }

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
            downParty.ShowClientTab = false;
            downParty.ShowResourceTab = false;
            downParty.ShowClaimTransformTab = false;

            switch (oauthTabTypes)
            {
                case OAuthTabTypes.Client:
                    downParty.ShowClientTab = true;
                    break;
                case OAuthTabTypes.Resource:
                    downParty.ShowResourceTab = true;
                    break;
                case OAuthTabTypes.ClaimsTransform:
                    downParty.ShowClaimTransformTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowSamlTab(GeneralSamlDownPartyViewModel downParty, SamlTabTypes samlTabTypes)
        {
            downParty.ShowSamlTab = false;
            downParty.ShowClaimTransformTab = false;

            switch (samlTabTypes)
            {
                case SamlTabTypes.Saml:
                    downParty.ShowSamlTab = true;
                    break;
                case SamlTabTypes.ClaimsTransform:
                    downParty.ShowClaimTransformTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowTrackLinkTab(GeneralTrackLinkDownPartyViewModel downParty, TrackLinkTabTypes trackLinkTabTypes)
        {
            downParty.ShowTrackLinkTab = false;
            downParty.ShowClaimTransformTab = false;

            switch (trackLinkTabTypes)
            {
                case TrackLinkTabTypes.TrackLink:
                    downParty.ShowTrackLinkTab = true;
                    break;
                case TrackLinkTabTypes.ClaimsTransform:
                    downParty.ShowClaimTransformTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void UpdateAllowUpParties((IAllowUpPartyNames model, List<UpPartyLink> upPartyLinks) arg, bool addDefaultUpParty)
        {
            arg.model.AllowUpParties = arg.upPartyLinks;
            AddDefaultUpParty(arg.model.AllowUpParties, addDefaultUpParty);
        }

        public void RemoveAllowUpParty((IAllowUpPartyNames model, UpPartyLink upPartyLink) arg, bool addDefaultUpParty)
        {
            arg.model.AllowUpParties.RemoveAll(p => p.Name == arg.upPartyLink.Name && p.ProfileName == arg.upPartyLink.ProfileName);
            AddDefaultUpParty(arg.model.AllowUpParties, addDefaultUpParty);
        }

        private static void AddDefaultUpParty(List<UpPartyLink> allowUpParties, bool addDefaultUpParty)
        {
            if (addDefaultUpParty && allowUpParties.Count() <= 0)
            {
                allowUpParties.Add(new UpPartyLink { Name = Constants.DefaultLogin.Name });
            }
        }

        public async Task DownPartyCancelAsync(GeneralDownPartyViewModel downParty)
        {
            DownParty.Edit = false;
            await OnStateHasChanged.InvokeAsync(DownParty);
        }
    }
}
