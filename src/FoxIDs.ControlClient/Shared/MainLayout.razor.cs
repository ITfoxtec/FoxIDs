using FoxIDs.Infrastructure;
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
using Blazored.Toast.Services;

namespace FoxIDs.Client.Shared
{
    public partial class MainLayout
    {
        private Modal createTenantModal;
        private PageEditForm<CreateTenantViewModel> createTenantForm;
        private IEnumerable<PlanInfo> planInfoList;
        private bool createTenantWorking;
        private bool createTenantDone;
        private List<string> createTenantReceipt = new List<string>();
        private Modal createTrackModal;
        private PageEditForm<CreateTrackViewModel> createTrackForm;
        private bool createTrackShowAdvanced;
        private bool createTrackWorking;
        private bool createTrackDone;
        private List<string> createTrackReceipt = new List<string>();
        private PageEditForm<FilterTrackViewModel> selectTrackFilterForm;
        private bool selectTrackInitialized = false;
        private string selectTrackError;
        private IEnumerable<Track> selectTrackTasks;
        private int selectTrackTotalCount;
        private Modal myProfileModal;
        private bool myProfileMasterMasterLogin;
        private bool showMyProfileClaims;
        private IEnumerable<Claim> myProfileClaims;
        private string myProfileError;
        private Modal notAccessModal;
        private bool showFullVersion;

        [CascadingParameter]
        private Task<AuthenticationState> authenticationStateTask { get; set; }

        [Inject]
        public AuthenticationStateProvider authenticationStateProvider { get; set; }

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public ControlClientSettingLogic ControlClientSettingLogic { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Inject]
        public IToastService ToastService { get; set; }

        [Inject]
        public UserProfileLogic UserProfileLogic { get; set; }

        [Inject]
        public TrackSelectedLogic TrackSelectedLogic { get; set; }

        [Inject]
        public ServerErrorLogic ServerErrorLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Inject]
        public UserService UserService { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        private bool IsMasterTrack => RouteBindingLogic.IsMasterTrack;

        private bool EnableCreateNewTenant => ClientSettings.EnableCreateNewTenant;

        private bool RequestPayment => RouteBindingLogic.RequestPayment;

        private bool ShowTrackFilter => selectTrackTotalCount > 10;

        protected override async Task OnInitializedAsync()
        {
            var user = (await authenticationStateTask).User;
            if (user.Identity.IsAuthenticated)
            {
                await ControlClientSettingLogic.InitLoadAsync();
            }
            await RouteBindingLogic.InitRouteBindingAsync();
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnSelectTrackAsync += OnSelectTrackAsync;
            NotificationLogic.OnClientSettingLoaded += OnClientSettingLoaded;
            NotificationLogic.OnRequestPaymentUpdated += OnRequestPaymentUpdated;
        }

        protected void Dispose()
        {
            TrackSelectedLogic.OnSelectTrackAsync -= OnSelectTrackAsync;
            NotificationLogic.OnClientSettingLoaded -= OnClientSettingLoaded;
            NotificationLogic.OnRequestPaymentUpdated -= OnRequestPaymentUpdated;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (!await ServerErrorLogic.HasErrorAsync())
            {
                var user = (await authenticationStateTask).User;
                if (user.Identity.IsAuthenticated)
                {
                    await LoadAndSelectTracAsync();
                    myProfileClaims = user.Claims;
                    if (user.Claims.Where(c => c.Type == Constants.JwtClaimTypes.AuthMethodType && c.Value == Constants.DefaultLogin.Name).Any() &&
                        user.Claims.Where(c => c.Type == Constants.JwtClaimTypes.AuthMethod && c.Value == Constants.DefaultLogin.Name).Any() &&
                        user.Claims.Where(c => c.Type == JwtClaimTypes.Email).Any())
                    {
                        myProfileMasterMasterLogin = true;
                    }
                }
                else if (notAccessModal != null)
                {
                    notAccessModal.Show();
                }
            }
            await base.OnParametersSetAsync();
        }

        private async Task LogoutAsync()
        {
            await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLogoutAsync();
        }

        private async Task ShowCreateTenantModalAsync()
        {
            try
            {
                if (ClientSettings.EnablePayment && planInfoList == null)
                {
                    planInfoList = await HelpersService.GetPlanInfoAsync();
                }

                createTenantWorking = false;
                createTenantDone = false;
                createTenantReceipt = new List<string>();
                createTenantForm.Init();
                createTenantModal.Show();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
        }

        private Task OnSetAdministratorPasswordEmailChanged(bool enabled)
        {
            if (createTenantForm?.Model == null)
            {
                return Task.CompletedTask;
            }

            if (enabled)
            {
                createTenantForm.Model.AdministratorPassword = null;
                createTenantForm.Model.ChangeAdministratorPassword = false;
                createTenantForm.Model.ConfirmAdministratorAccount = true;
                createTenantForm.ClearFieldError(nameof(createTenantForm.Model.AdministratorPassword));
                createTenantForm.ClearFieldError(nameof(createTenantForm.Model.ChangeAdministratorPassword));
            }

            if (createTenantForm.EditContext != null)
            {
                createTenantForm.EditContext.NotifyFieldChanged(new FieldIdentifier(createTenantForm.Model, nameof(createTenantForm.Model.SetAdministratorPasswordEmail)));
                if (enabled)
                {
                    createTenantForm.EditContext.NotifyFieldChanged(new FieldIdentifier(createTenantForm.Model, nameof(createTenantForm.Model.AdministratorPassword)));
                    createTenantForm.EditContext.NotifyFieldChanged(new FieldIdentifier(createTenantForm.Model, nameof(createTenantForm.Model.ChangeAdministratorPassword)));
                    createTenantForm.EditContext.NotifyFieldChanged(new FieldIdentifier(createTenantForm.Model, nameof(createTenantForm.Model.ConfirmAdministratorAccount)));
                }
            }

            return Task.CompletedTask;
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
                if (createTenantForm.Model.SetAdministratorPasswordEmail)
                {
                    createTenantForm.Model.AdministratorPassword = null;
                    createTenantForm.Model.ChangeAdministratorPassword = false;
                    createTenantForm.Model.ConfirmAdministratorAccount = true;
                }
                await TenantService.CreateTenantAsync(createTenantForm.Model.Map<CreateTenantRequest>(afterMap =>
                {
                    afterMap.ControlClientBaseUri = RouteBindingLogic.GetBaseUri();
                }));
                createTenantDone = true;
                createTenantReceipt.Add("Tenant created.");
                createTenantReceipt.Add("Master environment with user repository created.");
                createTenantReceipt.Add("Master login authentication method created.");
                createTenantReceipt.Add("First master administrator user created.");
                createTenantReceipt.Add("Master Control API registration created.");
                createTenantReceipt.Add("Master Control client registration created.");
                createTenantReceipt.Add("Test environment with user repository created.");
                createTenantReceipt.Add("Production environment with user repository created.");

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
                track.AutoMapSamlClaims = true;
                var trackResponse = await TrackService.CreateTrackAsync(track);
                createTrackForm.Model.Name = trackResponse.Name;
                createTrackDone = true;
                createTrackReceipt.Add("Environment created.");
                createTrackReceipt.Add("User repository created.");
                createTrackReceipt.Add("Certificate created.");
                createTrackReceipt.Add("Login authentication method created.");

                if (selectTrackFilterForm.Model != null)
                {
                    selectTrackFilterForm.Model.FilterName = null;
                }
                await LoadSelectTrackAsync();
                await TrackSelectedLogic.TrackSelectedAsync(trackResponse);
                await UserProfileLogic.UpdateTrackAsync(trackResponse.Name);
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
            await LoadAndSelectTracAsync(forceSelect: true);
            StateHasChanged();
        }  

        private void OnClientSettingLoaded()
        {
            StateHasChanged();
        }

        private void OnRequestPaymentUpdated()
        {
            StateHasChanged();
        }

        private async Task OpenPaymentMethodAsync()
        {
            if (NavigationManager.Uri.EndsWith("tenant", StringComparison.OrdinalIgnoreCase))
            {
                await NotificationLogic.OpenPaymentMethodAsync();
            }
            else
            {
                if(TrackSelectedLogic.Track.Name != Constants.Routes.MasterTrackName)
                {
                    var masterTrack = await TrackService.GetTrackAsync(Constants.Routes.MasterTrackName);
                    await TrackSelectedLogic.TrackSelectedAsync(masterTrack);
                }
                NavigationManager.NavigateTo($"{await RouteBindingLogic.GetTenantNameAsync()}/tenant");
            }
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

                var userProfile = await UserProfileLogic.GetUserProfileAsync();
                if (!string.IsNullOrWhiteSpace(userProfile?.LastTrackName) && await SelectTrackAsync(userProfile.LastTrackName))
                {
                    return;
                }

                if (await SelectNotTrackAsync("-") || await SelectTrackAsync("-"))
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
                selectTrackTasks = (await TrackService.GetTracksAsync(null)).Data.OrderTracks();
                selectTrackTotalCount = selectTrackTasks.Count();
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
                selectTrackTasks = (await TrackService.GetTracksAsync(selectTrackFilterForm.Model.FilterName)).Data.OrderTracks();
                if (selectTrackFilterForm.Model.FilterName.IsNullOrWhiteSpace())
                {
                    selectTrackTotalCount = selectTrackTasks.Count();
                }
            }
            catch (FoxIDsApiException aex)
            {
                if (aex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    selectTrackFilterForm.SetFieldError(nameof(selectTrackFilterForm.Model.FilterName), aex.Message);
                }
                else
                {
                    ToastService.ShowError(aex.Message);
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError(ex.Message);
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

        private async Task<bool> SelectNotTrackAsync(string trackName)
        {
            var track = selectTrackTasks.Where(t => t.Name != trackName && t.Name != Constants.Routes.MasterTrackName).FirstOrDefault();
            if (track != null)
            {
                await SelectTrackAsync(track);
                return true;
            }
            return false;
        }

        private bool IsTrackCurrentlySelected(Track track) => TrackSelectedLogic?.Track != null && track != null && track.Name.Equals(TrackSelectedLogic.Track.Name, StringComparison.OrdinalIgnoreCase);

        private string GetTrackTitle(Track track)
        {
            if (track == null)
            {
                return string.Empty;
            }

            if (track.DisplayName.IsNullOrWhiteSpace())
            {
                var prodText = track.Name.GetProdTrackName();
                return prodText.IsNullOrWhiteSpace() ? track.Name : $"{track.Name} {prodText}";
            }

            if (!track.Name.Equals(track.DisplayName, StringComparison.InvariantCultureIgnoreCase))
            {
                return $"{track.DisplayName} ({track.Name})";
            }

            return track.DisplayName;
        }

        private Task OnTrackClickedAsync(Track track)
        {
            if (IsTrackCurrentlySelected(track))
            {
                return Task.CompletedTask;
            }

            return SelectTrackAsync(track);
        }

        private async Task SelectTrackAsync(Track track)
        {
            if (!RouteBindingLogic.IsMasterTenant)
            {
                await UserProfileLogic.UpdateTrackAsync(track.Name);
            }
            await TrackSelectedLogic.TrackSelectedAsync(track);
        }

        private void ToggleVersionDisplay()
        {
            showFullVersion = !showFullVersion;
        }

        public async Task ChangeMyPasswordAsync()
        {
            try
            {
                await UserService.UpdateMyUserAsync(new MyUser { ChangePassword = true });

                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync(prompt: IdentityConstants.AuthorizationServerPrompt.Login);

            }
            catch (TokenUnavailableException)
            {
                await(OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                myProfileError = ex.Message;
            }
        }
    }
}
