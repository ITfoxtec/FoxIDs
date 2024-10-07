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
using System.Linq;
using Microsoft.JSInterop;

namespace FoxIDs.Client.Pages
{
    public partial class MasterTenant
    {
        private PageEditForm<MasterTenantViewModel> tenantSettingsForm;
        private string deleteTenantError;
        private bool deleteTenantAcknowledge = false;
        private string savedCustomDomain;
        private string changePaymentError;
        private bool changePaymentWorking;
        private Modal changePaymentModal;
        private Modal tenantDeletedModal;
        private bool tenantWorking;

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
        public HelpersService HelpersService { get; set; }

        [Inject]
        public MyTenantService MyTenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTrack => Constants.Routes.MasterTrackName.Equals(TrackSelectedLogic.Track?.Name, StringComparison.OrdinalIgnoreCase);

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
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        protected override void OnDispose()
        {
            TrackSelectedLogic.OnTrackSelectedAsync -= OnTrackSelectedAsync;
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
            try
            {
                tenantWorking = false;
                deleteTenantError = null;
                deleteTenantAcknowledge = false;

                var planInfoList = await HelpersService.GetPlanInfoAsync();

                var myTenant = await MyTenantService.GetTenantAsync();
                savedCustomDomain = myTenant.CustomDomain;
                //if(myTenant.Payment?.IsActive != true)
                //{
                //    showChangePlanPayment = true;
                //}
                await tenantSettingsForm.InitAsync(myTenant.Map<MasterTenantViewModel>(afterMap: afterMap => 
                {
                    if (!afterMap.PlanName.IsNullOrWhiteSpace())
                    {
                        var planDisplayName = planInfoList.Where(p => p.Name == afterMap.PlanName).Select(p => p.DisplayName).FirstOrDefault();
                        if (!planDisplayName.IsNullOrWhiteSpace())
                        {
                            afterMap.PlanName = planDisplayName;
                        }
                    }
                }));
                RouteBindingLogic.SetMyTenant(myTenant);
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

        private void ShowPlanModal()
        {
        }

        private async Task ShowPaymentModalAsync()
        {
            changePaymentError = null;
            changePaymentWorking = false;
            //createTrackReceipt = new List<string>();
            changePaymentModal.Show();

            await LoadMollieAsync();
        }

        private async Task HidePaymentModalAsync()
        {
            changePaymentModal.Hide();
            await UnloadMollieAsync();
        }

        private async Task LoadMollieAsync()
        {
            await JSRuntime.InvokeVoidAsync("loadMollie", ClientSettings.MollieProfileId, ClientSettings.PlanPaymentTestMode);
        }

        private async Task UnloadMollieAsync()
        {
            await JSRuntime.InvokeVoidAsync("unloadMollie", ClientSettings.MollieProfileId, ClientSettings.PlanPaymentTestMode);
        }

        private async Task SubmitMollieAsync()
        {
            var result = await JSRuntime.InvokeAsync<MolliePaymentResult>("submitMollie", ClientSettings.MollieProfileId, ClientSettings.PlanPaymentTestMode);

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
            try
            {
                deleteTenantAcknowledge = false;
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
