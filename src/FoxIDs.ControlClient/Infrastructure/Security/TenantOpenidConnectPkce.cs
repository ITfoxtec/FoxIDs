using Blazored.SessionStorage;
using FoxIDs.Client.Logic;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Client.Infrastructure.Security
{
    public class TenantOpenidConnectPkce : OpenidConnectPkce
    {
        private readonly RouteBindingLogic routeBindingBase;

        public TenantOpenidConnectPkce(IServiceProvider serviceProvider, RouteBindingLogic routeBindingBase, OpenidConnectPkceSettings globalOpenidClientPkceSettings, NavigationManager NavigationManager, ISessionStorageService sessionStorage, AuthenticationStateProvider authenticationStateProvider) : base(serviceProvider, globalOpenidClientPkceSettings, NavigationManager, sessionStorage, authenticationStateProvider)
        {
            this.routeBindingBase = routeBindingBase;
        }

        public async Task TenantLoginAsync()
        {
            var openidConnectPkceSettings = new OpenidConnectPkceSettings
            {
                Authority = await GetAuthority(),
                ClientId = globalOpenidClientPkceSettings.ClientId,
                ResponseMode = globalOpenidClientPkceSettings.ResponseMode,
                Scope = GetScope(),
                LoginCallBackPath = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.LoginCallBackPath),
                LogoutCallBackPath = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.LogoutCallBackPath)
            };

            await LoginAsync(openidConnectPkceSettings);
        }

        public async Task TenantLogoutAsync()
        {
            var openidConnectPkceSettings = new OpenidConnectPkceSettings
            {
                Authority = await GetAuthority(),
                ClientId = globalOpenidClientPkceSettings.ClientId,
                ResponseMode = globalOpenidClientPkceSettings.ResponseMode,
                Scope = GetScope(),
                LoginCallBackPath = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.LoginCallBackPath),
                LogoutCallBackPath = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.LogoutCallBackPath)
            };

            await LogoutAsync(openidConnectPkceSettings);
        }

        private async Task<string> ReplaceTenantNameAsync(string value)
        {
            return value.Replace("{tenant_name}", await routeBindingBase.GetTenantNameAsync(), StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> GetAuthority()
        {
            var authority = globalOpenidClientPkceSettings.Authority.Replace("{client_id}", globalOpenidClientPkceSettings.ClientId, StringComparison.OrdinalIgnoreCase);
            authority = await ReplaceTenantNameAsync(authority);
            return authority.Replace("https://foxids_endpoint", (globalOpenidClientPkceSettings as TenantOpenidConnectPkceSettings).FoxIDsEndpoint, StringComparison.OrdinalIgnoreCase);
        }

        private string GetScope()
        {
            if(routeBindingBase.IsMasterTenant)
            {
                return $"{globalOpenidClientPkceSettings.Scope} {(globalOpenidClientPkceSettings as TenantOpenidConnectPkceSettings).MasterScope}";
            }
            else
            {
                return globalOpenidClientPkceSettings.Scope;
            }
        }
    }
}
