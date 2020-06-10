using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models.ViewModels;
using FoxIDs.Shared.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Shared
{
    public partial class MainLayout
    {
        private Modal createTenantModal;
        private PageEditForm<CreateTenantViewModel> createTenantForm;
        private Modal myProfileModal;
        private IEnumerable<Claim> myProfileClaims;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Inject]
        public IAuthorizationService AuthorizationService { get; set; }

        [CascadingParameter]
        private Task<AuthenticationState> authenticationStateTask { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await RouteBindingLogic.InitRouteBindingAsync();
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            var user = (await authenticationStateTask).User;
            if (user.Identity.IsAuthenticated)
            {
                myProfileClaims = user.Claims;
            }
            await base.OnParametersSetAsync();
        }

        private async Task OnCreateTenantValidSubmitAsync(EditContext editContext)
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
