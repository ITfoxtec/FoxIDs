using Blazored.Toast.Services;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages.Usage
{
    public partial class UsageSettings
    {
        private string usageHref;
        private string usageTenantsHref;
        private GeneralUsageSettingsViewModel generalUsageSettings = new GeneralUsageSettingsViewModel();

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            usageHref = $"{TenantName}/usage";
            usageTenantsHref = $"{TenantName}/usagetenants";
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            generalUsageSettings.Error = null;

            try
            {
                var usageSettings = await TenantService.GetUsageSettingsAsync();
                await generalUsageSettings.Form.InitAsync(usageSettings.Map<UsageSettingsViewModel>());
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalUsageSettings.Error = ex.Message;
            }
        }

        private void AddItem(MouseEventArgs e, List<UsageCurrencyExchange> items)
        {
            items.Add(new UsageCurrencyExchange());
        }

        private void RemoveItem(MouseEventArgs e, List<UsageCurrencyExchange> items, UsageCurrencyExchange item)
        {
            items.Remove(item);
        }

        private async Task OnUpdateUsageSettingsValidSubmitAsync(EditContext editContext)
        {
            try
            {
                var usageSettings = await TenantService.UpdateUsageSettingsAsync(generalUsageSettings.Form.Model);
                toastService.ShowSuccess("Usage settings updated.");
            }
            catch (Exception ex)
            {
                generalUsageSettings.Form.SetError(ex.Message);
            }
        }
    }
}
