using FoxIDs.Client.Models.ViewModels;
using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Net.Http;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ELoginUpParty : UpPartyBase
    {
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
                var generalLoginUpParty = UpParty as GeneralLoginUpPartyViewModel;                
                var loginUpParty = await UpPartyService.GetLoginUpPartyAsync(UpParty.Name);
                await generalLoginUpParty.Form.InitAsync(ToViewModel(loginUpParty));
                if (generalLoginUpParty.ShowCreateUserTab && !loginUpParty.EnableCreateUser)
                {
                    generalLoginUpParty.ShowCreateUserTab = false;
                    generalLoginUpParty.ShowLoginTab = true;
                }
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

        private LoginUpPartyViewModel ToViewModel(LoginUpParty loginUpParty)
        {
            loginUpParty.ExtendedUis.MapExtendedUis();

            return loginUpParty.Map<LoginUpPartyViewModel>(afterMap: afterMap =>
            {
                afterMap.InitName = afterMap.Name;

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapOAuthClaimTransforms();
                }

                afterMap.ExtendedUis.MapExtendedUis();

                if (afterMap.ExitClaimTransforms?.Count > 0)
                {
                    afterMap.ExitClaimTransforms = afterMap.ExitClaimTransforms.MapOAuthClaimTransforms();
                }
                if (afterMap.CreateUser?.ClaimTransforms?.Count > 0)
                {
                    afterMap.CreateUser.ClaimTransforms = afterMap.CreateUser.ClaimTransforms.MapOAuthClaimTransforms();
                }
            });
        }

        private async Task LoginUpPartyViewModelAfterInitAsync(GeneralLoginUpPartyViewModel loginParty, LoginUpPartyViewModel model)
        {
            if (loginParty.CreateMode)
            {
                model.Name = await UpPartyService.GetNewPartyNameAsync();
            }

            if (model.TwoFactorAppName.IsNullOrWhiteSpace())
            {
                model.TwoFactorAppName = TenantName;
            }
        }

        private async Task OnEditLoginUpPartyValidSubmitAsync(GeneralLoginUpPartyViewModel generalLoginUpParty, EditContext editContext)
        {
            try
            {
                generalLoginUpParty.Form.Model.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalLoginUpParty.Form.Model.ExtendedUis.MapExtendedUisBeforeMap(); 
                generalLoginUpParty.Form.Model.ExitClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalLoginUpParty.Form.Model.CreateUser?.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();

                if (generalLoginUpParty.CreateMode)
                {
                    var loginUpPartyResult = await UpPartyService.CreateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>(afterMap: afterMap =>
                    {
                        afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();

                        if (afterMap.CreateUser != null)
                        {
                            afterMap.CreateUser.Elements.MapDynamicElementsAfterMap();
                            afterMap.CreateUser.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                        }
                        afterMap.ExtendedUis.MapExtendedUisAfterMap();
                        afterMap.ExitClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    }));
                    generalLoginUpParty.Form.UpdateModel(ToViewModel(loginUpPartyResult));
                    generalLoginUpParty.CreateMode = false;
                    toastService.ShowSuccess("Login application created.");
                    generalLoginUpParty.Name = loginUpPartyResult.Name;
                    generalLoginUpParty.DisplayName = loginUpPartyResult.DisplayName;
                }
                else
                {
                    var loginUpParty = await UpPartyService.UpdateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>(afterMap: afterMap =>
                    {
                        if (generalLoginUpParty.Form.Model.Name != generalLoginUpParty.Form.Model.InitName)
                        {
                            afterMap.NewName = afterMap.Name;
                            afterMap.Name = generalLoginUpParty.Form.Model.InitName;
                        }

                        afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                        if (afterMap.CreateUser != null)
                        {
                            afterMap.CreateUser.Elements.MapDynamicElementsAfterMap();
                            afterMap.CreateUser.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                        }
                        afterMap.ExtendedUis.MapExtendedUisAfterMap();
                        afterMap.ExitClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    }));
                    generalLoginUpParty.Form.UpdateModel(ToViewModel(loginUpParty));
                    toastService.ShowSuccess("Login application updated.");
                    generalLoginUpParty.Name = loginUpParty.Name;
                    generalLoginUpParty.DisplayName = loginUpParty.DisplayName;
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalLoginUpParty.Form.SetFieldError(nameof(generalLoginUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteLoginUpPartyAsync(GeneralLoginUpPartyViewModel generalLoginUpParty)
        {
            try
            {
                await UpPartyService.DeleteLoginUpPartyAsync(generalLoginUpParty.Name);
                UpParties.Remove(generalLoginUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalLoginUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
