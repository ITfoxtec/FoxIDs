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
            upParty.ShowLoginTab = false;
            upParty.ShowClaimTransformTab = false;
            upParty.ShowLoginUiTab = false;
            upParty.ShowExtendedUiTab = false;
            upParty.ShowCreateUserTab = false;
            upParty.ShowSessionTab = false;
            upParty.ShowHrdTab = false;

            switch (loginTabTypes)
            {
                case LoginTabTypes.Login:
                    upParty.ShowLoginTab = true;
                    break;
                case LoginTabTypes.ClaimsTransform:
                    upParty.ShowClaimTransformTab = true;
                    break;
                case LoginTabTypes.LoginUi:
                    upParty.ShowLoginUiTab = true;
                    break;
                case LoginTabTypes.ExtendedUi:
                    upParty.ShowExtendedUiTab = true;
                    break;
                case LoginTabTypes.CreateUser:
                    InitCreateUser(upParty);
                    upParty.ShowCreateUserTab = true;
                    break;
                case LoginTabTypes.Session:
                    upParty.ShowSessionTab = true;
                    break;
                case LoginTabTypes.Hrd:
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

                if (!(generalLoginUpParty.Form.Model.DisablePasswordAuth == true))
                {
                    generalLoginUpParty.Form.Model.CreateUser.Elements.Add(new DynamicElementViewModel
                    {
                        Type = DynamicElementTypes.Password,
                        Required = true
                    });
                }

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
            upParty.ShowClientTab = false;
            upParty.ShowClaimTransformTab = false;
            upParty.ShowExtendedUiTab = false;
            upParty.ShowLinkExternalUserTab = false;
            upParty.ShowHrdTab = false;
            upParty.ShowProfileTab = false;
            upParty.ShowSessionTab = false;

            switch (oauthTabTypes)
            {
                case OAuthTabTypes.Client:
                    upParty.ShowClientTab = true;
                    break;
                case OAuthTabTypes.ClaimsTransform:
                    upParty.ShowClaimTransformTab = true;
                    break;
                case OAuthTabTypes.ExtendedUi:
                    upParty.ShowExtendedUiTab= true;
                    break;
                case OAuthTabTypes.LinkExternalUser:
                    upParty.ShowLinkExternalUserTab = true;
                    break;
                case OAuthTabTypes.Hrd:
                    upParty.ShowHrdTab = true;
                    break;
                case OAuthTabTypes.Profile:
                    upParty.ShowProfileTab = true;
                    break;
                case OAuthTabTypes.Session:
                    upParty.ShowSessionTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowSamlTab(GeneralSamlUpPartyViewModel upParty, SamlTabTypes samlTabTypes)
        {
            upParty.ShowSamlTab = false;
            upParty.ShowClaimTransformTab = false;
            upParty.ShowExtendedUiTab = false;
            upParty.ShowLinkExternalUserTab = false;
            upParty.ShowHrdTab = false;
            upParty.ShowProfileTab = false;
            upParty.ShowSessionTab = false;

            switch (samlTabTypes)
            {
                case SamlTabTypes.Saml:
                    upParty.ShowSamlTab = true;
                    break;
                case SamlTabTypes.ClaimsTransform:
                    upParty.ShowClaimTransformTab = true;
                    break;
                case SamlTabTypes.ExtendedUi:
                    upParty.ShowExtendedUiTab = true;
                    break;
                case SamlTabTypes.LinkExternalUser:
                    upParty.ShowLinkExternalUserTab = true;
                    break;
                case SamlTabTypes.Hrd:
                    upParty.ShowHrdTab = true;
                    break;
                case SamlTabTypes.Profile:
                    upParty.ShowProfileTab = true;
                    break;
                case SamlTabTypes.Session:
                    upParty.ShowSessionTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowTrackLinkTab(GeneralTrackLinkUpPartyViewModel upParty, TrackLinkTabTypes trackLinkTabTypes)
        {
            upParty.ShowTrackLinkTab = false;
            upParty.ShowClaimTransformTab = false;
            upParty.ShowExtendedUiTab = false;
            upParty.ShowLinkExternalUserTab = false;
            upParty.ShowHrdTab = false;
            upParty.ShowProfileTab = false;
            upParty.ShowSessionTab = false;

            switch (trackLinkTabTypes)
            {
                case TrackLinkTabTypes.TrackLink:
                    upParty.ShowTrackLinkTab = true;
                    break;
                case TrackLinkTabTypes.ClaimsTransform:
                    upParty.ShowClaimTransformTab = true;
                    break;
                case TrackLinkTabTypes.ExtendedUi:
                    upParty.ShowExtendedUiTab = true;
                    break;
                case TrackLinkTabTypes.LinkExternalUser:
                    upParty.ShowLinkExternalUserTab = true;
                    break;
                case TrackLinkTabTypes.Hrd:
                    upParty.ShowHrdTab = true;
                    break;
                case TrackLinkTabTypes.Profile:
                    upParty.ShowProfileTab = true;
                    break;
                case TrackLinkTabTypes.Session:
                    upParty.ShowSessionTab = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void ShowExternalLoginTab(GeneralExternalLoginUpPartyViewModel upParty, ExternalLoginTabTypes externalLoginTabTypes)
        {
            upParty.ShowExternalLoginTab = false;
            upParty.ShowClaimTransformTab = false;
            upParty.ShowExtendedUiTab = false;
            upParty.ShowLinkExternalUserTab = false;
            upParty.ShowHrdTab = false;
            upParty.ShowProfileTab = false;
            upParty.ShowSessionTab = false;

            switch (externalLoginTabTypes)
            {
                case ExternalLoginTabTypes.ExternalLogin:
                    upParty.ShowExternalLoginTab = true;
                    break;
                case ExternalLoginTabTypes.ClaimsTransform:
                    upParty.ShowClaimTransformTab = true;
                    break;
                case ExternalLoginTabTypes.ExtendedUi:
                    upParty.ShowExtendedUiTab = true;
                    break;
                case ExternalLoginTabTypes.LinkExternalUser:
                    upParty.ShowLinkExternalUserTab = true;
                    break;
                case ExternalLoginTabTypes.Hrd:
                    upParty.ShowHrdTab = true;
                    break;
                case ExternalLoginTabTypes.Profile:
                    upParty.ShowProfileTab = true;
                    break;
                case ExternalLoginTabTypes.Session:
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

        protected string GetUpPartyDisplayName(IUpPartyName upParty)
        {
            var displayName = upParty.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = upParty.Name;
            }

            return displayName;
        }
    }
}