using Blazored.Toast.Services;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages.Usage
{
    public partial class UsageTenants
    {
        private string usageHref;
        private string usageSettingsHref;
        private FoxIDs.Models.Api.UsageSettings usageSettings;
        private PageEditForm<FilterTenantViewModel> searchTenantForm;
        private List<GeneralTenantViewModel> tenants;
        private string paginationToken;
        private bool tenantWorking;

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }


        protected override async Task OnInitializedAsync()
        {
            usageHref = $"{TenantName}/usage";
            usageSettingsHref = $"{TenantName}/usagesettings";
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task OnTenantFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralTenants(await TenantService.GetUsageTenantsAsync(searchTenantForm.Model.FilterValue));
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

        private async Task LoadMoreTenantAsync()
        {
            try
            {
                SetGeneralTenants(await TenantService.GetUsageTenantsAsync(searchTenantForm.Model.FilterValue, paginationToken: paginationToken), addTenants: true);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
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
            searchTenantForm?.ClearError();
            try
            {
                SetGeneralTenants(await TenantService.GetUsageTenantsAsync(null));
                usageSettings = await TenantService.GetUsageSettingsAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                tenants?.Clear();
                searchTenantForm.SetError(ex.Message);
            }
        }

        private void SetGeneralTenants(PaginationResponse<Tenant> dataTenans, bool addTenants = false)
        {
            var tes = new List<GeneralTenantViewModel>();
            foreach (var dp in dataTenans.Data)
            {
                tes.Add(new GeneralTenantViewModel(dp));
            }
            if (tenants != null && addTenants)
            {
                tenants.AddRange(tes);
            }
            else
            {
                tenants = tes;
            }
            paginationToken = dataTenans.PaginationToken;
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

        private void ShowCreateTenant()
        {
            var used = new GeneralTenantViewModel
            {
                CreateMode = true,
                Edit = true
            };

            tenants.Add(used);
        }

        private void TenantCancel(GeneralTenantViewModel tenant)
        {
            if (tenant.CreateMode)
            {
                tenants.Remove(tenant);
            }
            else
            {
                tenant.Edit = false;
            }
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

                if (generalTenant.CreateMode)
                {
                    var tenantResult = await TenantService.CreateTenantAsync(generalTenant.Form.Model.Map<CreateTenantRequest>(afterMap: afterMap => 
                    {
                        afterMap.ForUsage = true;
                        afterMap.AdministratorEmail = afterMap.Customer?.InvoiceEmails?.FirstOrDefault();
                        afterMap.AdministratorPassword = Util.SecretGenerator.GenerateNewPassword();
                        afterMap.ControlClientBaseUri = RouteBindingLogic.GetBaseUri();
                    }));
                    generalTenant.Form.UpdateModel(tenantResult.Map<TenantViewModel>());
                    generalTenant.CreateMode = false;
                    toastService.ShowSuccess("Usage tenant created.");
                }
                else
                {
                    var tenantResult = await TenantService.UpdateTenantAsync(generalTenant.Form.Model.Map<TenantRequest>(afterMap: afterMap => 
                    {
                        afterMap.ForUsage = true;
                    }));
                    generalTenant.Form.UpdateModel(tenantResult.Map<TenantViewModel>());
                    toastService.ShowSuccess("Usage tenant updated.");
                }

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
                if (tenantWorking)
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
    }
}
