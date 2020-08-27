using Blazored.SessionStorage;
using ITfoxtec.Identity;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class RouteBindingLogic
    {
        private const string tenanSessionKey = "tenant_session";
        private string tenantName;
        private bool? isMastertenant;
        private readonly NavigationManager NavigationManager;
        private readonly ISessionStorageService sessionStorage;
        private readonly AuthenticationStateProvider authenticationStateProvider;

        public RouteBindingLogic(NavigationManager NavigationManager, ISessionStorageService sessionStorage, AuthenticationStateProvider authenticationStateProvider)
        {
            this.NavigationManager = NavigationManager;
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

        public string GetPage()
        {
            var urlSplit = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Split('/');
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
            return NavigationManager.BaseUri;
        }

        public async Task InitRouteBindingAsync()
        {
            var urlSplit = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Split('/');
            tenantName = urlSplit[0];
            await ValidateAndUpdateSessionTenantName();
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
