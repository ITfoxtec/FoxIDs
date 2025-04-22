using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models;
using System;
using Blazored.Toast.Services;
using System.Linq;
using FoxIDs.Models.Api;

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
        public MetadataLogic MetadataLogic { get; set; }

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Parameter]
        public EventCallback<GeneralUpPartyViewModel> OnStateHasChanged { get; set; }

        [Parameter]
        public EventCallback<GeneralUpPartyViewModel> OnTestUpParty { get; set; }

        [Parameter]
        public List<GeneralUpPartyViewModel> UpParties { get; set; }

        [Parameter]
        public GeneralUpPartyViewModel UpParty { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        public void ShowLoginTab(GeneralLoginUpPartyViewModel upParty, LoginTabTypes loginTabTypes)
        {
            switch (loginTabTypes)
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
                    InitCreateUser(upParty);
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

        private void InitCreateUser(GeneralLoginUpPartyViewModel generalLoginUpParty)
        {
            if (generalLoginUpParty.Form.Model.CreateUser == null)
            {
                generalLoginUpParty.Form.Model.CreateUser = new CreateUserViewModel { ConfirmAccount = false };
            }
            if (generalLoginUpParty.Form.Model.CreateUser.Elements?.Any() != true)
            {
                generalLoginUpParty.Form.Model.CreateUser.Elements = new List<DynamicElementViewModel>();

                if (generalLoginUpParty.Form.Model.EnableUsernameIdentifier)
                {
                    generalLoginUpParty.Form.Model.CreateUser.Elements.Add(new DynamicElementViewModel
                    {
                        Type = DynamicElementTypes.Username,
                        Required = true,
                        IsUserIdentifier = true
                    });
                }
                if (generalLoginUpParty.Form.Model.EnablePhoneIdentifier)
                {
                    generalLoginUpParty.Form.Model.CreateUser.Elements.Add(new DynamicElementViewModel
                    {
                        Type = DynamicElementTypes.Phone,
                        Required = true,
                        IsUserIdentifier = true
                    });
                }
                if (generalLoginUpParty.Form.Model.EnableEmailIdentifier || (!generalLoginUpParty.Form.Model.EnablePhoneIdentifier && !generalLoginUpParty.Form.Model.EnableUsernameIdentifier))
                {
                    generalLoginUpParty.Form.Model.CreateUser.Elements.Add(new DynamicElementViewModel
                    {
                        Type = DynamicElementTypes.Email,
                        Required = true,
                        IsUserIdentifier = true
                    });
                }

                generalLoginUpParty.Form.Model.CreateUser.Elements.Add(new DynamicElementViewModel
                {
                    Type = DynamicElementTypes.Password,
                    Required = true
                });

                generalLoginUpParty.Form.Model.CreateUser.Elements.Add(new DynamicElementViewModel
                {
                    Type = DynamicElementTypes.GivenName
                });
                generalLoginUpParty.Form.Model.CreateUser.Elements.Add(new DynamicElementViewModel
                {
                    Type = DynamicElementTypes.FamilyName
                });
            }
        }

        public void ShowOAuthTab(IGeneralOAuthUpPartyTabViewModel upParty, OAuthTabTypes oauthTabTypes)
        {
            switch (oauthTabTypes)
            {
                case OAuthTabTypes.Client:
                    upParty.ShowClientTab = true;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case OAuthTabTypes.ClaimsTransform:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = true;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case OAuthTabTypes.LinkExternalUser:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = true;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case OAuthTabTypes.Hrd:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = true;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case OAuthTabTypes.Profile:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = true;
                    upParty.ShowSessionTab = false;
                    break;
                case OAuthTabTypes.Session:
                    upParty.ShowClientTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = true;
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
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case SamlTabTypes.ClaimsTransform:
                    upParty.ShowSamlTab = false;
                    upParty.ShowClaimTransformTab = true;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case SamlTabTypes.LinkExternalUser:
                    upParty.ShowSamlTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = true;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case SamlTabTypes.Hrd:
                    upParty.ShowSamlTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = true;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case SamlTabTypes.Profile:
                    upParty.ShowSamlTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = true;
                    upParty.ShowSessionTab = false;
                    break;
                case SamlTabTypes.Session:
                    upParty.ShowSamlTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = true;
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
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case TrackLinkTabTypes.ClaimsTransform:
                    upParty.ShowTrackLinkTab = false;
                    upParty.ShowClaimTransformTab = true;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case TrackLinkTabTypes.LinkExternalUser:
                    upParty.ShowTrackLinkTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = true;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case TrackLinkTabTypes.Hrd:
                    upParty.ShowTrackLinkTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = true;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case TrackLinkTabTypes.Profile:
                    upParty.ShowTrackLinkTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = true;
                    upParty.ShowSessionTab = false;
                    break;
                case TrackLinkTabTypes.Session:
                    upParty.ShowTrackLinkTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowExternalLoginTab(GeneralExternalLoginUpPartyViewModel upParty, ExternalLoginTabTypes externalLoginTabTypes)
        {
            switch (externalLoginTabTypes)
            {
                case ExternalLoginTabTypes.ExternalLogin:
                    upParty.ShowExternalLoginTab = true;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case ExternalLoginTabTypes.ClaimsTransform:
                    upParty.ShowExternalLoginTab = false;
                    upParty.ShowClaimTransformTab = true;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case ExternalLoginTabTypes.LinkExternalUser:
                    upParty.ShowExternalLoginTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = true;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case ExternalLoginTabTypes.Hrd:
                    upParty.ShowExternalLoginTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = true;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = false;
                    break;
                case ExternalLoginTabTypes.Profile:
                    upParty.ShowExternalLoginTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = true;
                    upParty.ShowSessionTab = false;
                    break;
                case ExternalLoginTabTypes.Session:
                    upParty.ShowExternalLoginTab = false;
                    upParty.ShowClaimTransformTab = false;
                    upParty.ShowLinkExternalUserTab = false;
                    upParty.ShowHrdTab = false;
                    upParty.ShowProfileTab = false;
                    upParty.ShowSessionTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
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
