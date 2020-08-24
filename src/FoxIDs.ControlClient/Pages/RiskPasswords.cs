using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class RiskPasswords
    {
        private string riskPasswordLoadError;
        private RiskPasswordInfo riskPasswordInfo;
        //private List<GeneralTrackCertificateViewModel> certificates = new List<GeneralTrackCertificateViewModel>();

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

    }
}
