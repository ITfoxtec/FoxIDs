using FoxIDs.Infrastructure;
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

namespace FoxIDs.Client.Pages
{
    public partial class RiskPasswords
    {
        private string riskPasswordLoadError;
        private RiskPasswordInfo uploadRiskPassword { get; set; }
        private PageEditForm<TestRiskPasswordViewModel> testRiskPasswordForm { get; set; }

        [Inject]
        public RiskPasswordService RiskPasswordService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
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
