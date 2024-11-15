using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Threading.Tasks;
using FoxIDs.Models.Api;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.Config;

namespace FoxIDs.Client.Pages.Settings
{
    public partial class RiskPasswords
    {
        private string trackSettingsHref;
        private string mailSettingsHref;
        private string claimMappingsHref;
        private string textsHref;
        private string plansHref;

        private string riskPasswordLoadError;
        private RiskPasswordInfo uploadRiskPassword { get; set; }
        private PageEditForm<TestRiskPasswordViewModel> testRiskPasswordForm { get; set; }

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public RiskPasswordService RiskPasswordService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        protected override async Task OnInitializedAsync()
        {
            trackSettingsHref = $"{TenantName}/envsettings";
            mailSettingsHref = $"{TenantName}/mailsettings";
            claimMappingsHref = $"{TenantName}/claimmappings";
            textsHref = $"{TenantName}/texts";
            plansHref = $"{TenantName}/plans";
            await base.OnInitializedAsync();
            await DefaultRiskPasswordLoadAsync();
        }

        private async Task DefaultRiskPasswordLoadAsync()
        {
            riskPasswordLoadError = null;
            try
            {
                uploadRiskPassword = await RiskPasswordService.GetRiskPasswordInfoAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                riskPasswordLoadError = ex.Message;
            }
        }

        private async Task OnTestRiskPasswordValidSubmitAsync(EditContext editContext)
        {
            try
            {
                var passwordInRisk = await RiskPasswordService.GetRiskPasswordTestAsync(testRiskPasswordForm.Model.Password);
                testRiskPasswordForm.Model.IsValid = !passwordInRisk;
            }
            catch
            {
                testRiskPasswordForm.Model.IsValid = true;
                throw;
            }
        }
    }
}
