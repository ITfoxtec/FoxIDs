using FoxIDs.Infrastructure;
using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using Blazored.Toast.Services;
using System.Net.Http;
using ITfoxtec.Identity;
using FoxIDs.Client.Models.Config;

namespace FoxIDs.Client.Pages
{
    public partial class Tenants : IDisposable 
    {
        private PageEditForm<FilterTenantViewModel> searchTenantForm;
        private List<GeneralTenantViewModel> tenants;
        private bool tenantWorking;
        private IEnumerable<PlanInfo> planInfoList;

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
            NotificationLogic.OnTenantUpdatedAsync += OnTenantUpdatedAsync;
        }

        private async Task OnTenantUpdatedAsync()
        {
            await DefaultLoadAsync();
            StateHasChanged();
        }

        private async Task OnTenantFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralTenants(await TenantService.FilterTenantAsync(searchTenantForm.Model.FilterValue));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    searchTenantForm.SetFieldError(nameof(searchTenantForm.Model.FilterValue), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                SetGeneralTenants(await TenantService.FilterTenantAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                searchTenantForm.SetError(ex.Message);
            }
        }

        private void SetGeneralTenants(IEnumerable<Tenant> dataTenans)
        {
            var tes = new List<GeneralTenantViewModel>();
            foreach (var dp in dataTenans)
            {
                tes.Add(new GeneralTenantViewModel(dp) 
                {
                    LoginUri = $"{RouteBindingLogic.GetBaseUri().Trim('/')}/{dp.Name}".ToLower()
                });
            }
            tenants = tes;
        }

        private async Task ShowUpdateTenantAsync(GeneralTenantViewModel generalTenant)
        {
            tenantWorking = false;
            generalTenant.DeleteAcknowledge = false;
            generalTenant.ShowAdvanced = false;
            generalTenant.Error = null;
            generalTenant.Edit = true;

            try
            {
                if (planInfoList == null)
                {
                    planInfoList = await HelpersService.GetPlanInfoAsync();
                }

                var tenant = await TenantService.GetTenantAsync(generalTenant.Name);
                await generalTenant.Form.InitAsync(tenant.Map<TenantViewModel>());
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalTenant.Error = ex.Message;
            }
        }

        private string TenantInfoText(GeneralTenantViewModel generalTenant)
        {
            return $"{generalTenant.Name}{(!generalTenant.CustomDomain.IsNullOrEmpty() ? $" - custom domain: '{generalTenant.CustomDomain}'{(generalTenant.CustomDomainVerified ? " verified" : string.Empty)}" : string.Empty)}";
        }

        private async Task OnEditTenantValidSubmitAsync(GeneralTenantViewModel generalTenant, EditContext editContext)
        {
            try
            {
                if (tenantWorking)
                {
                    return;
                }
                tenantWorking = true;
                var tenantResult = await TenantService.UpdateTenantAsync(generalTenant.Form.Model.Map<TenantRequest>());
                generalTenant.Form.UpdateModel(tenantResult.Map<TenantViewModel>());
                toastService.ShowSuccess("Tenant updated.");

                generalTenant.CustomDomain = generalTenant.Form.Model.CustomDomain;
                generalTenant.CustomDomainVerified = generalTenant.Form.Model.CustomDomainVerified;
                tenantWorking = false;
            }
            catch (FoxIDsApiException ex)
            {
                tenantWorking = false;
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalTenant.Form.SetFieldError(nameof(generalTenant.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteTenantAsync(GeneralTenantViewModel generalTenant)
        {
            try
            {
                generalTenant.DeleteAcknowledge = false;                
                if(tenantWorking)
                {
                    return;
                }
                tenantWorking = true;
                await TenantService.DeleteTenantAsync(generalTenant.Name);
                tenants.Remove(generalTenant);
                tenantWorking = false;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                tenantWorking = false;
                generalTenant.Form.SetError(ex.Message);
            }
        }

        public void Dispose()
        {
            NotificationLogic.OnTenantUpdatedAsync -= OnTenantUpdatedAsync;
        }
    }
}
