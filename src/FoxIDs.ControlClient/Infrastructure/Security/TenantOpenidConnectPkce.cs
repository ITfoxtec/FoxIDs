using Blazored.SessionStorage;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.Config;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Threading.Tasks;
using static ITfoxtec.Identity.IdentityConstants;

namespace FoxIDs.Client.Infrastructure.Security
{
    public class TenantOpenidConnectPkce : OpenidConnectPkce
    {
        private readonly RouteBindingLogic routeBindingBase;
        private readonly ControlClientSettingLogic controlClientSettingLogic;
        private readonly ClientSettings clientSettings;

        public TenantOpenidConnectPkce(IServiceProvider serviceProvider, RouteBindingLogic routeBindingBase, ControlClientSettingLogic controlClientSettingLogic, ClientSettings clientSettings, OpenidConnectPkceSettings globalOpenidClientPkceSettings, NavigationManager NavigationManager, ISessionStorageService sessionStorage, AuthenticationStateProvider authenticationStateProvider) : base(serviceProvider, globalOpenidClientPkceSettings, NavigationManager, sessionStorage, authenticationStateProvider)
        {
            this.routeBindingBase = routeBindingBase;
            this.controlClientSettingLogic = controlClientSettingLogic;
            this.clientSettings = clientSettings;
        }

        public async Task TenantLoginAsync()
        {
            await controlClientSettingLogic.InitLoadAsync();

            var openidConnectPkceSettings = new OpenidConnectPkceSettings
            {
                Authority = await GetAuthority(),
                ClientId = Constants.ControlClient.ClientId,
                ResponseMode = globalOpenidClientPkceSettings.ResponseMode,
                Scope = GetScope(),
                LoginCallBackPath = await ReplaceTenantNameAsync(clientSettings.LoginCallBackPath),
                LogoutCallBackPath = await ReplaceTenantNameAsync(clientSettings.LogoutCallBackPath)
            };

            await LoginAsync(openidConnectPkceSettings);
        }

        public async Task TenantLogoutAsync()
        {
            await controlClientSettingLogic.InitLoadAsync();

            var openidConnectPkceSettings = new OpenidConnectPkceSettings
            {
                Authority = await GetAuthority(),
                ClientId = Constants.ControlClient.ClientId,
                ResponseMode = globalOpenidClientPkceSettings.ResponseMode,
                Scope = GetScope(),
                LoginCallBackPath = await ReplaceTenantNameAsync(clientSettings.LoginCallBackPath),
                LogoutCallBackPath = await ReplaceTenantNameAsync(clientSettings.LogoutCallBackPath)
            };

            await LogoutAsync(openidConnectPkceSettings);
        }

        private async Task<string> ReplaceTenantNameAsync(string value)
        {
            return value.Replace("{tenant_name}", await routeBindingBase.GetTenantNameAsync(), StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> GetAuthority()
        {
            var authority = clientSettings.Authority.Replace("{client_id}", Constants.ControlClient.ClientId, StringComparison.OrdinalIgnoreCase);
            authority = await ReplaceTenantNameAsync(authority);
            return authority.Replace("{foxids_endpoint}", clientSettings.FoxIDsEndpoint, StringComparison.OrdinalIgnoreCase);
        }

        private string GetScope()
        {
            if(routeBindingBase.IsMasterTenant)
            {
                return $"{DefaultOidcScopes.OfflineAccess} {DefaultOidcScopes.Email} {Constants.ControlApi.ResourceAndScope.Tenant} {Constants.ControlApi.ResourceAndScope.Master}";
            }
            else
            {
                return $"{DefaultOidcScopes.OfflineAccess} {DefaultOidcScopes.Email} {Constants.ControlApi.ResourceAndScope.Tenant}";
            }
        }
    }
}
