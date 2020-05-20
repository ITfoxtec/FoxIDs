using Blazored.SessionStorage;
using FoxIDs.Logic;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Security
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
                Authority = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.Authority),
                ClientId = globalOpenidClientPkceSettings.ClientId,
                ResponseMode = globalOpenidClientPkceSettings.ResponseMode,
                LoginCallBackPath = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.LoginCallBackPath),
                LogoutCallBackPath = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.LogoutCallBackPath)
            };

            await LoginAsync(openidConnectPkceSettings);
        }

        public async Task TenantLogoutAsync()
        {
            var openidConnectPkceSettings = new OpenidConnectPkceSettings
            {
                Authority = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.Authority),
                ClientId = globalOpenidClientPkceSettings.ClientId,
                ResponseMode = globalOpenidClientPkceSettings.ResponseMode,
                LoginCallBackPath = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.LoginCallBackPath),
                LogoutCallBackPath = await ReplaceTenantNameAsync(globalOpenidClientPkceSettings.LogoutCallBackPath)
            };

            await LogoutAsync(openidConnectPkceSettings);
        }

        private async Task<string> ReplaceTenantNameAsync(string value)
        {
            return value.Replace("{tenant_name}", await routeBindingBase.GetTenantNameAsync(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
