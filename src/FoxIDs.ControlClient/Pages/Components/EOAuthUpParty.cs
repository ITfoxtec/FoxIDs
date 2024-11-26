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
    public partial class EOAuthUpParty : UpPartyBase
    {
        protected List<string> responseTypeItems = new List<string> (Constants.OAuth.DefaultResponseTypes);

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
                var generalOAuthUpParty = UpParty as GeneralOAuthUpPartyViewModel;
                var oauthUpParty = await UpPartyService.GetOAuthUpPartyAsync(UpParty.Name);
                await generalOAuthUpParty.Form.InitAsync(ToViewModel(generalOAuthUpParty, oauthUpParty));
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

        private OAuthUpPartyViewModel ToViewModel(GeneralOAuthUpPartyViewModel generalOAuthUpParty, OAuthUpParty oauthUpParty)
        {

            return oauthUpParty.Map<OAuthUpPartyViewModel>(afterMap =>
            {
                afterMap.InitName = afterMap.Name;

                if (afterMap.DisplayName.IsNullOrWhiteSpace())
                {
                    afterMap.DisplayName = afterMap.Name;
                }

                if (oauthUpParty.UpdateState == PartyUpdateStates.Manual)
                {
                    afterMap.IsManual = true;
                }

                if (oauthUpParty.UpdateState == PartyUpdateStates.AutomaticStopped)
                {
                    afterMap.AutomaticStopped = true;
                }
                else
                {
                    afterMap.AutomaticStopped = false;
                }

                if (afterMap.Client != null)
                {
                    afterMap.Client.Party = afterMap;
                }

                generalOAuthUpParty.KeyInfoList.Clear();
                foreach (var key in afterMap.Keys)
                {
                    if (key.Kty == MTokens.JsonWebAlgorithmsKeyTypes.RSA && key.X5c?.Count >= 1)
                    {
                        generalOAuthUpParty.KeyInfoList.Add(new KeyInfoViewModel
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
                        generalOAuthUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            KeyId = key.Kid,
                            Key = key
                        });
                    }
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapOAuthClaimTransforms();
                }
            });
        }

        private async Task OAuthUpPartyViewModelAfterInitAsync(GeneralOAuthUpPartyViewModel oauthUpParty, OAuthUpPartyViewModel model)
        {
            if (oauthUpParty.CreateMode)
            {
                model.Name = await UpPartyService.GetNewPartyNameAsync();
                if (oauthUpParty.TokenExchange)
                {
                    model.DisableUserAuthenticationTrust = true;
                }
                model.Client = new OAuthUpClientViewModel();
                model.Client.Claims = new List<string> { "*" };
            }

            if (model.Client != null && model.Client.Party == null)
            {
                model.Client.Party = model;
            }
        }

        private async Task OnEditOAuthUpPartyValidSubmitAsync(GeneralOAuthUpPartyViewModel generalOAuthUpParty, EditContext editContext)
        {
            try
            {
                generalOAuthUpParty.Form.Model.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();

                var oauthUpParty = generalOAuthUpParty.Form.Model.Map<OAuthUpParty>(afterMap: afterMap =>
                {
                    afterMap.UpdateState = PartyUpdateStates.Automatic;

                    afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                });

                if (generalOAuthUpParty.CreateMode)
                {
                    var oauthUpPartyResult = await UpPartyService.CreateOAuthUpPartyAsync(oauthUpParty);
                    generalOAuthUpParty.Form.UpdateModel(ToViewModel(generalOAuthUpParty, oauthUpPartyResult));
                    generalOAuthUpParty.CreateMode = false;
                    toastService.ShowSuccess("OAuth 2.0 application created.");
                    generalOAuthUpParty.Name = oauthUpPartyResult.Name;
                    generalOAuthUpParty.DisplayName = oauthUpPartyResult.DisplayName;
                }
                else
                {
                    if (generalOAuthUpParty.Form.Model.Name != generalOAuthUpParty.Form.Model.InitName)
                    {
                        oauthUpParty.NewName = oauthUpParty.Name;
                        oauthUpParty.Name = generalOAuthUpParty.Form.Model.InitName;
                    }
                    var oauthUpPartyResult = await UpPartyService.UpdateOAuthUpPartyAsync(oauthUpParty);
                    generalOAuthUpParty.Form.UpdateModel(ToViewModel(generalOAuthUpParty, oauthUpPartyResult));
                    toastService.ShowSuccess("OAuth 2.0 application updated.");
                    generalOAuthUpParty.Name = oauthUpPartyResult.Name;
                    generalOAuthUpParty.DisplayName = oauthUpPartyResult.DisplayName;
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalOAuthUpParty.Form.SetFieldError(nameof(generalOAuthUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteOAuthUpPartyAsync(GeneralOAuthUpPartyViewModel generalOAuthUpParty)
        {
            try
            {
                await UpPartyService.DeleteOAuthUpPartyAsync(generalOAuthUpParty.Name);
                UpParties.Remove(generalOAuthUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalOAuthUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
