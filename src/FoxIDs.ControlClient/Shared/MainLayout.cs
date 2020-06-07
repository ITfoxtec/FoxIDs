using FoxIDs.Logic;
using FoxIDs.Models.ViewModels;
using FoxIDs.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Threading.Tasks;

namespace FoxIDs.Shared
{
    public partial class MainLayout
    {
        private Modal createTenantModal;
        private EditContext createTenantEditContext;
        private string createTenantError;
        private CreateTenantViewModel createTenant;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await RouteBindingLogic.InitRouteBindingAsync();
        }

        protected override void OnInitialized()
        {
            CreateTenantModalInitialization();
        }

        private void CreateTenantModalInitialization()
        {
            createTenantError = null;
            createTenant = new CreateTenantViewModel();
            createTenantEditContext = new EditContext(createTenant);
        }

        private async Task OnSubmitAsync()
        {
            createTenantError = null;
            var isValid = createTenantEditContext.Validate();

            if (isValid)
            {
                try
                {
                    await TenantService.CreateTenantAsync(createTenant);
                }
                catch (FoxIDs.Infrastructure.FoxIDsApiException ex)
                {
                    var messageStore = new ValidationMessageStore(createTenantEditContext);
                    if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        messageStore.Add(createTenantEditContext.Field(nameof(createTenant.Name)), ex.Message);
                    }
                    else
                    {
                        createTenantError = ex.Message;
                    }
                }


            }
            /*
             * create master track
             * admin user
             */
        }
    }
}
