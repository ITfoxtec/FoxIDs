using Blazored.SessionStorage;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Client.Logic
{
    public class RouteBindingLogic
    {
        private const string tenanSessionKey = "tenant_session";
        private string tenantName;
        private Tenant tenant;
        private bool? isMastertenant;
        private readonly ClientSettings clientSettings;
        private readonly IServiceProvider serviceProvider;
        private readonly NavigationManager navigationManager;
        private readonly ISessionStorageService sessionStorage;
        private readonly AuthenticationStateProvider authenticationStateProvider;

        public RouteBindingLogic(ClientSettings clientSettings, IServiceProvider serviceProvider, NavigationManager navigationManager, ISessionStorageService sessionStorage, AuthenticationStateProvider authenticationStateProvider)
        {
            this.clientSettings = clientSettings;
            this.serviceProvider = serviceProvider;
            this.navigationManager = navigationManager;
            this.sessionStorage = sessionStorage;
            this.authenticationStateProvider = authenticationStateProvider;
        }

        public bool IsMasterTenant => (isMastertenant ?? (isMastertenant = "master".Equals(tenantName, StringComparison.OrdinalIgnoreCase))).Value;

        public async Task<string> GetTenantNameAsync()
        {
            if(tenantName.IsNullOrEmpty())
            {
                await InitRouteBindingAsync();
            }
            return tenantName;
        }

        public string GetFoxIDsTenantEndpoint()
        {
            if (!IsMasterTenant && tenant.CustomDomainVerified)
            {
                return $"https://{tenant.CustomDomain}";
            }
            else
            {
                return $"{clientSettings.FoxIDsEndpoint}/{tenantName}";
            }
        }

        public string GetPage()
        {
            var urlSplit = navigationManager.ToBaseRelativePath(navigationManager.Uri).Split('/');
            if(urlSplit.Count() > 1) 
            {
                return urlSplit[1];
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetBaseUri()
        {
            return navigationManager.BaseUri;
        }

        public async Task InitRouteBindingAsync()
        {
            var urlSplit = navigationManager.ToBaseRelativePath(navigationManager.Uri).Split('/');
            tenantName = urlSplit[0];
            await ValidateAndUpdateSessionTenantName();
            if (!IsMasterTenant)
            {
                await LoadMyTenantAsync();
            }
        }

        private async Task ValidateAndUpdateSessionTenantName()
        {
            var tenanSession = await sessionStorage.GetItemAsync<string>(tenanSessionKey);
            if(tenanSession == null)
            {
                await sessionStorage.SetItemAsync(tenanSessionKey, tenantName);
            }
            else
            {
                if (!tenanSession.Equals(tenantName, StringComparison.OrdinalIgnoreCase))
                {
                    await (authenticationStateProvider as OidcAuthenticationStateProvider).DeleteSessionAsync(true);
                    await sessionStorage.SetItemAsync(tenanSessionKey, tenantName);
                }
            }
        }

        private async Task LoadMyTenantAsync()
        {
            if (!IsMasterTenant)
            {
                var myTenantService = serviceProvider.GetService<MyTenantService>();
                tenant = await myTenantService.GetTenantAsync();
            }
        }
    }
}
