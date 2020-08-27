﻿using FoxIDs.Client.Infrastructure;
using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Authentication;
using FoxIDs.Client.Infrastructure.Security;

namespace FoxIDs.Client.Pages
{
    public partial class Tenants : IDisposable 
    {
        private PageEditForm<FilterTenantViewModel> searchTenantForm;
        private IEnumerable<Tenant> tenants = new List<Tenant>();
        private Modal tenantModal;
        private TenantInfoViewModel tenantInfo = new TenantInfoViewModel();
        private string deleteTenantError;
        private bool deleteTenantAcknowledge = false;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadTenentsAsync();
            NotificationLogic.OnTenantUpdatedAsync += OnTenantUpdatedAsync;
        }

        private async Task OnTenantUpdatedAsync()
        {
            try
            {
                await OnValidSubmitAsync(null);
                StateHasChanged();
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                searchTenantForm.SetError(ex.Message);
            }
        }

        private async Task OnValidSubmitAsync(EditContext editContext)
        {
            try
            {
                tenants = await TenantService.FilterTenantAsync(searchTenantForm.Model.FilterName);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    searchTenantForm.SetFieldError(nameof(searchTenantForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DefaultLoadTenentsAsync()
        {
            try
            {
                tenants = await TenantService.FilterTenantAsync(null);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                searchTenantForm.SetError(ex.Message);
            }
        }

        private void ShowTenant(string tenantName)
        {
            tenantInfo.Name = tenantName;
            tenantInfo.LoginUri = $"{RouteBindingLogic.GetBaseUri().Trim('/')}/{tenantName}".ToLower();
            deleteTenantError = null;
            deleteTenantAcknowledge = false;
            tenantModal.Show();
        }

        private async Task DeleteTenantAsync(string tenantName)
        {
            try
            {
                await TenantService.DeleteTenantAsync(tenantName);
                await OnTenantUpdatedAsync();
                tenantModal.Hide();
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                deleteTenantError = ex.Message;
            }
        }

        public void Dispose()
        {
            NotificationLogic.OnTenantUpdatedAsync -= OnTenantUpdatedAsync;
        }
    }
}
