using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models.Api;
using FoxIDs.Models.ViewModels;
using FoxIDs.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Pages
{
    public partial class Tenants
    {
        private Modal searchTenantModal;
        private PageEditForm<SearchTenantViewModel> searchTenantForm;
        private IEnumerable<Tenant> tenants = new List<Tenant>();

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadTenentsAsync();
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
                    var messageStore = new ValidationMessageStore(editContext);
                    messageStore.Add(editContext.Field(nameof(searchTenantForm.Model.Name)), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DefaultLoadTenentsAsync()
        {
            try
            {
                tenants = await TenantService.SearchTenantAsync(null);
            }
            catch (Exception ex)
            {
                searchTenantForm.SetError(ex.Message);
            }
        }
    }
}
