using FoxIDs.Client.Infrastructure;
using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Security.Authentication;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;

namespace FoxIDs.Client.Shared
{
    public partial class MainLayout
    {
        private Modal createTenantModal;
        private PageEditForm<CreateTenantViewModel> createTenantForm;
        private bool createTenantDone;
        private List<string> createTenantReceipt = new List<string>();
        private Modal createTrackModal;
        private PageEditForm<CreateTrackViewModel> createTrackForm;
        private bool createTrackDone;
        private List<string> createTrackReceipt = new List<string>();
        private Modal updateTrackModal;
        private PageEditForm<UpdateTrackViewModel> updateTrackForm;
        private string deleteTrackError;
        private bool deleteTrackAcknowledge = false;
        private PageEditForm<FilterTrackViewModel> selectTrackFilterForm;
        private Modal selectTrackModal;
        private bool selectTrackInitialized = false;
        private string selectTrackError;
        private IEnumerable<Track> selectTrackTasks;
        private Modal myProfileModal;
        private IEnumerable<Claim> myProfileClaims;

        [CascadingParameter]
        private Task<AuthenticationState> authenticationStateTask { get; set; }

        [Inject]
        public AuthenticationStateProvider authenticationStateProvider { get; set; }

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

        [Inject]
        public TrackSelectedLogic TrackSelectedLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

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
                await ShowSelectTrackModalAsync();
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
                await TenantService.CreateTenantAsync(createTenantForm.Model.Map<CreateTenantRequest>(afterMap =>
                {
                    afterMap.ControlClientBaseUri = RouteBindingLogic.GetBaseUri();
                }));
                createTenantDone = true;
                createTenantReceipt.Add("Tenant created.");
                createTenantReceipt.Add("Master track with user repository created.");
                createTenantReceipt.Add("Master track default Login Up-party created.");
                createTenantReceipt.Add("First master track administrator user created.");
                createTenantReceipt.Add("Master track control api down-party created.");
                createTenantReceipt.Add("Master track control client down-party created.");

                await NotificationLogic.TenantUpdatedAsync();
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

        private void ShowCreateTrackModal()
        {
            createTrackDone = false;
            createTrackReceipt = new List<string>();
            createTrackForm.Init();
            createTrackModal.Show();
        }

        private async Task OnCreateTrackValidSubmitAsync(EditContext editContext)
        {
            try
            {
                var track = createTrackForm.Model.Map<Track>();
                await TrackService.CreateTrackAsync(track);
                createTrackDone = true;
                createTrackReceipt.Add("Track created.");
                createTrackReceipt.Add("User repository created.");
                createTrackReceipt.Add("Default Login Up-party created.");

                if (selectTrackFilterForm.Model != null)
                {
                    selectTrackFilterForm.Model.FilterName = null;
                }
                await LoadSelectTrackAsync();
                await TrackSelectedLogic.TrackSelectedAsync(track);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    createTrackForm.SetFieldError(nameof(createTrackForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task ShowUpdateTrackModalAsync()
        {
            deleteTrackError = null;
            deleteTrackAcknowledge = false;
            var track = await TrackService.GetTrackAsync(TrackSelectedLogic.Track.Name);
            await updateTrackForm.InitAsync(track.Map<UpdateTrackViewModel>());
            updateTrackModal.Show();
        }

        private async Task OnUpdateTrackValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TrackService.UpdateTrackAsync(updateTrackForm.Model.Map<Track>());
                updateTrackModal.Hide();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    updateTrackForm.SetFieldError(nameof(updateTrackForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteTrackAsync()
        {
            try
            {
                await TrackService.DeleteTrackAsync(TrackSelectedLogic.Track.Name);
                updateTrackModal.Hide();
                if (selectTrackFilterForm.Model != null)
                {
                    selectTrackFilterForm.Model.FilterName = null;
                }
                await LoadSelectTrackAsync();
                selectTrackModal.Show();
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                deleteTrackError = ex.Message;
            }
        }


        private async Task ShowSelectTrackModalAsync()
        {
            if (selectTrackModal == null || selectTrackInitialized || TrackSelectedLogic.IsTrackSelected)
            {
                return;
            }
            selectTrackInitialized = true;

            if (RouteBindingLogic.IsMasterTenant)
            {
                try
                {
                    await SelectTrackAsync(await TrackService.GetTrackAsync(Constants.Routes.MasterTrackName));
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
            }
            else
            {
                selectTrackError = null;
                selectTrackModal.Show();
                await LoadSelectTrackAsync();
            }
        }

        private async Task LoadSelectTrackAsync()
        {
            try
            {
                selectTrackTasks = await TrackService.FilterTrackAsync(null);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                selectTrackError = ex.Message;
            }
        }

        private async Task OnSelectTrackFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                selectTrackTasks = await TrackService.FilterTrackAsync(selectTrackFilterForm.Model.FilterName);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    selectTrackFilterForm.SetFieldError(nameof(selectTrackFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task SelectTrackAsync(Track track)
        {
            await TrackSelectedLogic.TrackSelectedAsync(track);
            selectTrackModal.Hide();
        }
    }
}
