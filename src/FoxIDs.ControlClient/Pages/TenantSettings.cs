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
    public partial class TenantSettings
    {
        private string trackSettingsHref;
        private string mailSettingsHref;
        private string claimMappingsHref;
        private PageEditForm<TenantSettingsViewModel> tenantSettingsForm;
        private string deleteTenantError;
        private bool deleteTenantAcknowledge = false;
        private string savedCustomDomain;
        private Modal tenantDeletedModal;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public MyTenantService MyTenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        private bool IsCustomDomainVerified
        {
            get 
            {
                return tenantSettingsForm != null && tenantSettingsForm.Model.CustomDomainVerified && !savedCustomDomain.IsNullOrEmpty() && savedCustomDomain.Equals(tenantSettingsForm.Model.CustomDomain, StringComparison.OrdinalIgnoreCase);
            }
            set { }
        }

        private bool IsMasterTrack => Constants.Routes.MasterTrackName.Equals(TrackSelectedLogic.Track.Name, StringComparison.OrdinalIgnoreCase);

        protected override async Task OnInitializedAsync()
        {
            trackSettingsHref = $"{TenantName}/tracksettings";
            mailSettingsHref = $"{TenantName}/mailsettings";
            claimMappingsHref = $"{TenantName}/claimmappings";
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task OnTrackSelectedAsync(Track track)
        {
            await DefaultLoadAsync();
            StateHasChanged();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                deleteTenantError = null;
                deleteTenantAcknowledge = false;
                var myTenant = await MyTenantService.GetTenantAsync();
                savedCustomDomain = myTenant.CustomDomain;
                await tenantSettingsForm.InitAsync(myTenant.Map<TenantSettingsViewModel>());
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
                var myTenant = await MyTenantService.UpdateTenantAsync(tenantSettingsForm.Model.Map<MyTenantRequest>());
                toastService.ShowSuccess("Tenant settings updated.");
                savedCustomDomain = myTenant.CustomDomain;
                tenantSettingsForm.Model.CustomDomain = myTenant.CustomDomain;
                tenantSettingsForm.Model.CustomDomainVerified = myTenant.CustomDomainVerified;
                RouteBindingLogic.SetMyTenant(myTenant);
            }
            catch (Exception ex)
            {
                tenantSettingsForm.SetError(ex.Message);
            }
        }

        private async Task DeleteTenantAsync()
        {
            try
            {
                await MyTenantService.DeleteTenantAsync();
                tenantDeletedModal.Show();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                deleteTenantError = ex.Message;
            }
        }
    }
}
