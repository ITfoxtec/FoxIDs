using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using Blazored.Toast.Services;
using ITfoxtec.Identity;
using FoxIDs.Client.Models.Config;
using FoxIDs.Infrastructure;
using Microsoft.JSInterop;
using System.Collections.Generic;

namespace FoxIDs.Client.Pages
{
    public partial class MasterTenant
    {
        private PageEditForm<MasterTenantViewModel> tenantSettingsForm;
        private string deleteTenantError;
        private bool deleteTenantAcknowledge = false;
        private string deleteTenantAcknowledgeText = string.Empty;
        private string savedCustomDomain;
        private string changePaymentError;
        private bool changePaymentWorking;
        private Modal changePaymentModal;
        private Modal tenantDeletedModal;
        private bool tenantWorking;
        private TenantResponse myTenant;
        private IEnumerable<PlanInfo> planInfoList;


        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IToastService ToastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Inject]
        public MyTenantService MyTenantService { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTrack => RouteBindingLogic.IsMasterTrack;

        private bool IsCustomDomainVerified
        {
            get 
            {
                return tenantSettingsForm != null && tenantSettingsForm.Model.CustomDomainVerified && !savedCustomDomain.IsNullOrEmpty() && savedCustomDomain.Equals(tenantSettingsForm.Model.CustomDomain, StringComparison.OrdinalIgnoreCase);
            }
            set { }
        }

        protected override async Task OnInitializedAsync()
        {        
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            NotificationLogic.OnClientSettingLoaded += OnClientSettingLoaded;
            NotificationLogic.OnOpenPaymentMethodAsync += OnOpenPaymentMethodAsync;
            if (TrackSelectedLogic.Track.Name != Constants.Routes.MasterTrackName)
            {
                var masterTrack = await TrackService.GetTrackAsync(Constants.Routes.MasterTrackName);
                await TrackSelectedLogic.TrackSelectedAsync(masterTrack);
            }
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        protected override void OnDispose()
        {
            TrackSelectedLogic.OnTrackSelectedAsync -= OnTrackSelectedAsync;
            NotificationLogic.OnClientSettingLoaded -= OnClientSettingLoaded;
            NotificationLogic.OnOpenPaymentMethodAsync -= OnOpenPaymentMethodAsync;
            base.OnDispose();
        }

        private async Task OnTrackSelectedAsync(Track track)
        {
            if (!IsMasterTrack)
            {
                NavigationManager.NavigateTo($"{TenantName}/applications");
            }
            await DefaultLoadAsync();
            StateHasChanged();
        }

        private async Task DefaultLoadAsync()
        {
            tenantSettingsForm?.ClearError();
            try
            {
                tenantWorking = false;
                deleteTenantError = null;
                deleteTenantAcknowledge = false;

                myTenant = await MyTenantService.GetTenantAsync();
                RouteBindingLogic.SetMyTenant(myTenant);

                savedCustomDomain = myTenant.CustomDomain;
                
                await tenantSettingsForm.InitAsync(myTenant.Map<MasterTenantViewModel>());
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                tenantSettingsForm.SetError(ex.Message);
            }
        }

        private async Task OnUpdateTenantViewModelAfterInitAsync(MasterTenantViewModel model)
        {
            try
            {
                planInfoList = await HelpersService.GetPlanInfoAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
        }

        private async Task OnUpdateTenantValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if (tenantWorking)
                {
                    return;
                }
                tenantWorking = true;
                var myTenant = await MyTenantService.UpdateTenantAsync(tenantSettingsForm.Model.Map<MyTenantRequest>());
                ToastService.ShowSuccess("Tenant settings updated.");
                tenantSettingsForm.Model.PlanName = myTenant.PlanName;
                savedCustomDomain = myTenant.CustomDomain;
                tenantSettingsForm.Model.CustomDomain = myTenant.CustomDomain;
                tenantSettingsForm.Model.CustomDomainVerified = myTenant.CustomDomainVerified;
                RouteBindingLogic.SetMyTenant(myTenant);
                tenantWorking = false;
            }
            catch (Exception ex)
            {
                tenantWorking = false;
                tenantSettingsForm.SetError(ex.Message);
            }
        }

        private void OnClientSettingLoaded()
        {
            StateHasChanged();
        }

        private async Task OnOpenPaymentMethodAsync()
        {
            await ShowPaymentModalAsync();
        }

        private async Task ShowPaymentModalAsync()
        {
            try
            {
                (var isValid, var error) = await tenantSettingsForm.Submit();
                if (isValid)
                {
                    changePaymentError = null;
                    changePaymentWorking = false;
                    changePaymentModal.Show();

                    await LoadMollieAsync();
                }
                else if (!error.IsNullOrWhiteSpace())
                {
                    ToastService.ShowError(error);
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
        }

        private async Task HidePaymentModalAsync()
        {
            changePaymentModal.Hide();
            await UnloadMollieAsync();
        }

        private async Task LoadMollieAsync()
        {
            await JSRuntime.InvokeVoidAsync("loadMollie", ClientSettings.MollieProfileId, ClientSettings.PaymentTestMode);
        }

        private async Task UnloadMollieAsync()
        {
            await JSRuntime.InvokeVoidAsync("unloadMollie", ClientSettings.MollieProfileId, ClientSettings.PaymentTestMode);
        }

        private async Task SubmitMollieAsync()
        {
            var result = await JSRuntime.InvokeAsync<MolliePaymentResult>("submitMollie", ClientSettings.MollieProfileId, ClientSettings.PaymentTestMode);

            if (string.IsNullOrWhiteSpace(result.Error?.Detail) && !string.IsNullOrWhiteSpace(result.Error?.Message?.ToString()))
            {
                changePaymentError = result.Error?.Message?.ToString();
            }
            else if (!string.IsNullOrWhiteSpace(result.Token))
            {
                try
                {
                    changePaymentError = null;
                    var firstPaymentResponse = await MyTenantService.CreateMollieFirstPaymentAsync(new MollieFirstPaymentRequest { CardToken = result.Token });

                    if (!firstPaymentResponse.CheckoutUrl.IsNullOrWhiteSpace())
                    {
                        NavigationManager.NavigateTo(firstPaymentResponse.CheckoutUrl);
                    }
                    else
                    {
                        await HidePaymentModalAsync();
                    }
                }
                catch (FoxIDsApiException ex)
                {
                    changePaymentWorking = false;
                    changePaymentError = ex.Message;
                }
            }
            else
            {
                changePaymentError = null;
            }
        }

        private async Task DeleteTenantAsync()
        {
            deleteTenantError = string.Empty;
            if (!"delete".Equals(deleteTenantAcknowledgeText, StringComparison.InvariantCultureIgnoreCase))
            {
                deleteTenantError = "Please type 'delete' to confirm that you want to delete.";
                return;
            }

            try
            {
                deleteTenantAcknowledge = false;
                deleteTenantAcknowledgeText = string.Empty;
                if (tenantWorking)
                {
                    return;
                }
                tenantWorking = true;
                await MyTenantService.DeleteTenantAsync();
                tenantDeletedModal.Show();
                tenantWorking = false;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                tenantWorking = false;
                deleteTenantError = ex.Message;
            }
        }
    }
}
