﻿using FoxIDs.Infrastructure;
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
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.Config;
using System.Linq;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Shared
{
    public partial class MainLayout
    {
        private Modal createTenantModal;
        private PageEditForm<CreateTenantViewModel> createTenantForm;
        private bool createTenantWorking;
        private bool createTenantDone;
        private List<string> createTenantReceipt = new List<string>();
        private Modal createTrackModal;
        private PageEditForm<CreateTrackViewModel> createTrackForm;
        private bool createTrackWorking;
        private bool createTrackDone;
        private List<string> createTrackReceipt = new List<string>();
        private PageEditForm<FilterTrackViewModel> selectTrackFilterForm;
        private bool selectTrackInitialized = false;
        private string selectTrackError;
        private IEnumerable<Track> selectTrackTasks;
        private Modal myProfileModal;
        private IEnumerable<Claim> myProfileClaims;
        private Modal notAccessModal;

        [CascadingParameter]
        private Task<AuthenticationState> authenticationStateTask { get; set; }

        [Inject]
        public AuthenticationStateProvider authenticationStateProvider { get; set; }

        [Inject]
        public ClientSettings clientSettings { get; set; }

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
            TrackSelectedLogic.OnSelectTrackAsync += OnSelectTrackAsync;
        }

        protected override async Task OnParametersSetAsync()
        {
            var user = (await authenticationStateTask).User;
            if (user.Identity.IsAuthenticated && user.IsInRole(Constants.ControlApi.Role.TenantAdmin))
            {
                await LoadAndSelectTracAsync();
                myProfileClaims = user.Claims;
            }
            else if(notAccessModal != null)
            {
                notAccessModal.Show();
            }
            await base.OnParametersSetAsync();
        }

        private async Task LogoutAsync()
        {
            await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLogoutAsync();
        }

        private void ShowCreateTenantModal()
        {
            createTenantWorking = false;
            createTenantDone = false;
            createTenantReceipt = new List<string>();
            createTenantForm.Init(); 
            createTenantModal.Show();
        }

        private async Task OnCreateTenantValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if (createTenantWorking)
                {
                    return;
                }
                createTenantWorking = true;
                await TenantService.CreateTenantAsync(createTenantForm.Model.Map<CreateTenantRequest>(afterMap =>
                {
                    afterMap.ControlClientBaseUri = RouteBindingLogic.GetBaseUri();
                }));
                createTenantDone = true;
                createTenantReceipt.Add("Tenant created.");
                createTenantReceipt.Add("Master track with user repository created.");
                createTenantReceipt.Add("Master track default login up-party created.");
                createTenantReceipt.Add("First master track administrator user created.");
                createTenantReceipt.Add("Master track FoxIDs Control API down-party created.");
                createTenantReceipt.Add("Master track FoxIDs Control client down-party created.");
                createTenantReceipt.Add("Test track with user repository created.");
                createTenantReceipt.Add("Production track with user repository created.");

                await NotificationLogic.TenantUpdatedAsync();
            }
            catch (FoxIDsApiException ex)
            {
                createTenantWorking = false;
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
            createTrackWorking = false;
            createTrackDone = false;
            createTrackReceipt = new List<string>();
            createTrackForm.Init();
            createTrackModal.Show();
        }

        private async Task OnCreateTrackValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if(createTrackWorking)
                {
                    return;
                }
                createTrackWorking = true;
                var track = createTrackForm.Model.Map<Track>();
                await TrackService.CreateTrackAsync(track);
                createTrackDone = true;
                createTrackReceipt.Add("Track created.");
                createTrackReceipt.Add("User repository created.");
                createTrackReceipt.Add("Default login up-party created.");

                if (selectTrackFilterForm.Model != null)
                {
                    selectTrackFilterForm.Model.FilterName = null;
                }
                await LoadSelectTrackAsync();
                await TrackSelectedLogic.TrackSelectedAsync(track);
            }
            catch (FoxIDsApiException ex)
            {
                createTrackWorking = false;
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

        private async Task OnSelectTrackAsync()
        {
            await LoadAndSelectTracAsync(true);
            StateHasChanged();
        }

        private async Task LoadAndSelectTracAsync(bool forceSelect = false)
        {
            if (!forceSelect && (selectTrackInitialized || TrackSelectedLogic.IsTrackSelected))
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
                catch (TokenUnavailableException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
            }
            else
            {
                selectTrackError = null;
                await LoadSelectTrackAsync();

                var trackCookieName = await TrackSelectedLogic.ReadTrackCookieAsync();
                if (trackCookieName.IsNullOrEmpty() && await SelectTrackAsync(trackCookieName))
                {
                    return;
                }

                if (await SelectTrackAsync("test") || await SelectTrackAsync("dev") || await SelectTrackAsync("-"))
                {
                    return;
                }
                else if (selectTrackTasks.Count() > 1)
                {
                    await SelectTrackAsync(selectTrackTasks.Where(t => t.Name != Constants.Routes.MasterTrackName).First());
                    return;
                }
                else
                {
                    await SelectTrackAsync(selectTrackTasks.First());
                    return;
                }
            }
        }

        private async Task LoadSelectTrackAsync()
        {
            try
            {
                selectTrackError = null;
                selectTrackTasks = await TrackService.FilterTrackAsync(null);
                
            }
            catch (TokenUnavailableException)
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

        private async Task<bool> SelectTrackAsync(string trackName)
        {
            var track = selectTrackTasks.Where(t => t.Name == trackName).FirstOrDefault();
            if (track != null)
            {
                await SelectTrackAsync(track);
                return true;
            }
            return false;
        }

        private async Task SelectTrackAsync(Track track)
        {
            await TrackSelectedLogic.TrackSelectedAsync(track, RouteBindingLogic.IsMasterTenant);
        }
    }
}
