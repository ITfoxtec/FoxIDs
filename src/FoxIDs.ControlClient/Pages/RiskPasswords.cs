using FoxIDs.Client.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class RiskPasswords
    {
        private string riskPasswordLoadError;
        private RiskPasswordInfo riskPasswordInfo;
        private PageEditForm<TestRiskPasswordViewModel> testRiskPasswordForm { get; set; }

        [Inject]
        public RiskPasswordService RiskPasswordService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            riskPasswordLoadError = null;
            try
            {
                riskPasswordInfo = await RiskPasswordService.GetRiskPasswordInfoAsync();
            }
            catch (AuthenticationException)
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
                var passwordSha1Hash = testRiskPasswordForm.Model.Password.Sha1Hash();
                var riskPassword = await RiskPasswordService.GetRiskPasswordAsync(passwordSha1Hash);
                if(riskPassword != null)
                {
                    testRiskPasswordForm.Model.IsValid = false;
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    testRiskPasswordForm.Model.IsValid = true;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
