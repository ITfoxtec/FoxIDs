﻿using Blazored.Toast.Services;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
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

        public void ShowTrackLinkTab(GeneralTrackLinkDownPartyViewModel downParty, TrackLinkTabTypes trackLinkTabTypes)
        {
            switch (trackLinkTabTypes)
            {
                case TrackLinkTabTypes.TrackLink:
                    downParty.ShowTrackLinkTab = true;
                    downParty.ShowClaimTransformTab = false;
                    break;
                case TrackLinkTabTypes.ClaimsTransform:
                    downParty.ShowTrackLinkTab = false;
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

        public async Task DownPartyCancelAsync(GeneralDownPartyViewModel downParty)
        {
            DownParty.Edit = false;
            await OnStateHasChanged.InvokeAsync(DownParty);
        }
    }
}
