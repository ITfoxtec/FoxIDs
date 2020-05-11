using Blazored.SessionStorage;
using ITfoxtec.Identity;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class RouteBindingLogic
    {
        private const string tenanSessionKey = "tenant_session";
        private string tenantName;
        private readonly NavigationManager navigationManager;
        private readonly ISessionStorageService sessionStorage;
        private readonly AuthenticationStateProvider authenticationStateProvider;

        public RouteBindingLogic(NavigationManager navigationManager, ISessionStorageService sessionStorage, AuthenticationStateProvider authenticationStateProvider)
        {
            this.navigationManager = navigationManager;
            this.sessionStorage = sessionStorage;
            this.authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<string> GetTenantNameAsync()
        {
            if(tenantName.IsNullOrEmpty())
            {
                var urlSplit = navigationManager.ToBaseRelativePath(navigationManager.Uri).Split('/');
                tenantName = urlSplit[0];
                await ValidateAndUpdateSessionTenantName();
            }
            return tenantName;
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
                    await (authenticationStateProvider as OidcAuthenticationStateProvider).DeleteSessionAsync();
                    await sessionStorage.SetItemAsync(tenanSessionKey, tenantName);
                }
            }
        }
    }
}
