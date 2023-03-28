using FoxIDs.Client.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity;
using MTokens = Microsoft.IdentityModel.Tokens;
using System.Net.Http;

namespace FoxIDs.Client.Pages.Components
{
    public partial class EOidcUpParty : UpPartyBase
    {
        protected List<string> responseTypeItems = new List<string> (Constants.Oidc.DefaultResponseTypes);

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (!UpParty.CreateMode)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                var generalOidcUpParty = UpParty as GeneralOidcUpPartyViewModel;
                var oidcUpParty = await UpPartyService.GetOidcUpPartyAsync(UpParty.Name);
                await generalOidcUpParty.Form.InitAsync(ToViewModel(generalOidcUpParty, oidcUpParty));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                UpParty.Error = ex.Message;
            }
        }

        private OidcUpPartyViewModel ToViewModel(GeneralOidcUpPartyViewModel generalOidcUpParty, OidcUpParty oidcUpParty)
        {
            return oidcUpParty.Map<OidcUpPartyViewModel>(afterMap =>
            {
                if (oidcUpParty.UpdateState == PartyUpdateStates.Manual)
                {
                    afterMap.IsManual = true;
                }

                if (oidcUpParty.UpdateState == PartyUpdateStates.AutomaticStopped)
                {
                    afterMap.AutomaticStopped = true;
                }
                else
                {
                    afterMap.AutomaticStopped = false;
                }

                afterMap.EnableSingleLogout = !oidcUpParty.DisableSingleLogout;
                if (oidcUpParty.Client != null)
                {
                    afterMap.Client.EnableFrontChannelLogout = !oidcUpParty.Client.DisableFrontChannelLogout;
                }

                generalOidcUpParty.KeyInfoList.Clear();
                foreach (var key in afterMap.Keys)
                {
                    if (key.Kty == MTokens.JsonWebAlgorithmsKeyTypes.RSA && key.X5c?.Count >= 1)
                    {
                        generalOidcUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = key.CertificateInfo.Subject,
                            ValidFrom = key.CertificateInfo.ValidFrom,
                            ValidTo = key.CertificateInfo.ValidTo,
                            IsValid = key.CertificateInfo.IsValid(),
                            Thumbprint = key.CertificateInfo.Thumbprint,
                            KeyId = key.Kid,
                            Key = key
                        });
                    }
                    else
                    {
                        generalOidcUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            KeyId = key.Kid,
                            Key = key
                        });
                    }
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapClaimTransforms();
                }
            });
        }

        private void OidcUpPartyViewModelAfterInit(GeneralOidcUpPartyViewModel oidcUpParty, OidcUpPartyViewModel model)
        {
            if (oidcUpParty.CreateMode)
            {
                model.Client = new OidcUpClientViewModel();
                model.Client.Claims = new List<string> { "*" };
            }
        }

        private async Task OnEditOidcUpPartyValidSubmitAsync(GeneralOidcUpPartyViewModel generalOidcUpParty, EditContext editContext)
        {
            try
            {
                if(generalOidcUpParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalOidcUpParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is OAuthClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var oidcUpParty = generalOidcUpParty.Form.Model.Map<OidcUpParty>(afterMap: afterMap =>
                {
                    afterMap.UpdateState = PartyUpdateStates.Automatic;
                    afterMap.DisableSingleLogout = !generalOidcUpParty.Form.Model.EnableSingleLogout;
                    afterMap.Client.DisableFrontChannelLogout = !generalOidcUpParty.Form.Model.Client.EnableFrontChannelLogout;

                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                });

                if (generalOidcUpParty.CreateMode)
                {
                    var oidcUpPartyResult = await UpPartyService.CreateOidcUpPartyAsync(oidcUpParty);
                    generalOidcUpParty.Form.UpdateModel(ToViewModel(generalOidcUpParty, oidcUpPartyResult));
                    generalOidcUpParty.CreateMode = false;
                    toastService.ShowSuccess("OpenID Connect up-party created.");
                }
                else
                {
                    var oidcUpPartyResult = await UpPartyService.UpdateOidcUpPartyAsync(oidcUpParty);
                    generalOidcUpParty.Form.UpdateModel(ToViewModel(generalOidcUpParty, oidcUpPartyResult));
                    toastService.ShowSuccess("OpenID Connect up-party updated.");
                }

                generalOidcUpParty.Name = generalOidcUpParty.Form.Model.Name;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalOidcUpParty.Form.SetFieldError(nameof(generalOidcUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteOidcUpPartyAsync(GeneralOidcUpPartyViewModel generalOidcUpParty)
        {
            try
            {
                await UpPartyService.DeleteOidcUpPartyAsync(generalOidcUpParty.Name);
                UpParties.Remove(generalOidcUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalOidcUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
