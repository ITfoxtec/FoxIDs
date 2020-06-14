using FoxIDs.Client.Infrastructure;
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

namespace FoxIDs.Client.Pages
{
    public partial class Tenants : IDisposable 
    {
        private Modal searchTenantModal;
        private PageEditForm<SearchTenantViewModel> searchTenantForm;
        private IEnumerable<Tenant> tenants = new List<Tenant>();

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadTenentsAsync();
            NotificationLogic.OnTenantUpdatedAsync += OnTenantUpdatedAsync;
        }

        private async Task OnTenantUpdatedAsync()
        {
            await OnValidSubmitAsync(null);
            StateHasChanged();
        }

        private async Task OnValidSubmitAsync(EditContext editContext)
        {
            try
            {
                tenants = await TenantService.SearchTenantAsync(searchTenantForm.Model.Name);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    searchTenantForm.SetFieldError(nameof(searchTenantForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DefaultLoadTenentsAsync()
        {
            tenants = await TenantService.SearchTenantAsync(null);
        }

        public void Dispose()
        {
            NotificationLogic.OnTenantUpdatedAsync -= OnTenantUpdatedAsync;
        }
    }
}
