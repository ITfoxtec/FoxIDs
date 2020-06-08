using FoxIDs.Infrastructure;
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
        private PageEditForm<CreateTenantViewModel> createTenantForm;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await RouteBindingLogic.InitRouteBindingAsync();
            await base.OnInitializedAsync();
        }

        private async Task OnValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TenantService.CreateTenantAsync(createTenantForm.Model);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var messageStore = new ValidationMessageStore(editContext);
                    messageStore.Add(editContext.Field(nameof(createTenantForm.Model.Name)), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
