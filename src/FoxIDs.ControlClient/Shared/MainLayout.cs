using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models.Api;
using FoxIDs.Models.ViewModels;
using FoxIDs.Services;
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
        private bool createTenantDone;
        private List<string> createTenantReceipt = new List<string>();
        private Modal myProfileModal;
        private IEnumerable<Claim> myProfileClaims;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

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

        private void ShowCreateTenantModal()
        {
            createTenantDone = false;
            createTenantReceipt = new List<string>();
            createTenantForm.Init(); 
            createTenantModal.Show();
        }

        private async Task OnCreateTenantValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TenantService.CreateTenantAsync(new CreateTenantRequest 
                {
                    Name = createTenantForm.Model.Name, 
                    AdministratorEmail = createTenantForm.Model.AdministratorEmail,
                    AdministratorPassword = createTenantForm.Model.AdministratorPassword,
                    ControlClientBaseUri = RouteBindingLogic.GetBaseUri()
                });
                createTenantDone = true;
                createTenantReceipt.Add("Tenant created.");

                //await TrackService.CreateTrackAsync(new Track { Name = Constants.Routes.MasterTrackName }, createTenantForm.Model.Name);
                createTenantReceipt.Add("Master track created.");


                createTenantReceipt.Add("Master track default up party login created.");
                createTenantReceipt.Add("First master track administrator user created.");
                createTenantReceipt.Add("Master track down party control api created.");
                createTenantReceipt.Add("Master track down party control client created.");
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    createTenantForm.SetFieldError(nameof(createTenantForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }       
    }
}
