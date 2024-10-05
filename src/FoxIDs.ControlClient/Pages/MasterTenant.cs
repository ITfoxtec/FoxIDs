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

namespace FoxIDs.Client.Pages
{
    public partial class MasterTenant
    {
        private PageEditForm<MasterTenantViewModel> tenantSettingsForm;
        private string deleteTenantError;
        private bool deleteTenantAcknowledge = false;
        private string savedCustomDomain;
        private Modal tenantDeletedModal;
        private bool tenantWorking;

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IToastService ToastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

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
            await DefaultLoadAsync();
            if (!IsMasterTrack)
            {
                NavigationManager.NavigateTo($"{TenantName}/applications");
            }
            StateHasChanged();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                tenantWorking = false;
                deleteTenantError = null;
                deleteTenantAcknowledge = false;
                var myTenant = await MyTenantService.GetTenantAsync();
                savedCustomDomain = myTenant.CustomDomain;
                await tenantSettingsForm.InitAsync(myTenant.Map<MasterTenantViewModel>());
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
