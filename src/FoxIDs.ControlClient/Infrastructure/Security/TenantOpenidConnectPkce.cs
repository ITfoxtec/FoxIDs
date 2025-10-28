using Blazored.SessionStorage;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.Config;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using ITfoxtec.Identity.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ITfoxtec.Identity.IdentityConstants;

namespace FoxIDs.Client.Infrastructure.Security
{
    public class TenantOpenidConnectPkce : OpenidConnectPkce
    {
        private readonly RouteBindingLogic routeBindingBase;
        private readonly ControlClientSettingLogic controlClientSettingLogic;
        private readonly ClientSettings clientSettings;
        private readonly NavigationManager navigationManager;

        public TenantOpenidConnectPkce(IServiceProvider serviceProvider, RouteBindingLogic routeBindingBase, ControlClientSettingLogic controlClientSettingLogic, ClientSettings clientSettings, OpenidConnectPkceSettings globalOpenidClientPkceSettings, NavigationManager navigationManager, ISessionStorageService sessionStorage, OidcHelper oidcHelper, AuthenticationStateProvider authenticationStateProvider) : base(serviceProvider, globalOpenidClientPkceSettings, navigationManager, sessionStorage, oidcHelper, authenticationStateProvider)
        {
            this.routeBindingBase = routeBindingBase;
            this.controlClientSettingLogic = controlClientSettingLogic;
            this.clientSettings = clientSettings;
            this.navigationManager = navigationManager;
        }

        public async Task TenantLoginAsync(string prompt = null)
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

            var loginHint = GetLoginHintFromFragment();

            await LoginAsync(openidConnectPkceSettings, prompt: prompt, loginHint: loginHint);
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
                return $"{DefaultOidcScopes.Profile} {DefaultOidcScopes.OfflineAccess} {DefaultOidcScopes.Email} {Constants.ControlApi.ResourceAndScope.Tenant} {Constants.ControlApi.ResourceAndScope.Master}";
            }
            else
            {
                return $"{DefaultOidcScopes.Profile} {DefaultOidcScopes.OfflineAccess} {DefaultOidcScopes.Email} {Constants.ControlApi.ResourceAndScope.Tenant}";
            }
        }

        private string GetLoginHintFromFragment()
        {
            var uri = new Uri(navigationManager.Uri);
            if (string.IsNullOrEmpty(uri.Fragment) || uri.Fragment.Length <= 1)
            {
                return null;
            }

            var fragment = uri.Fragment.TrimStart('#');
            if (string.IsNullOrWhiteSpace(fragment))
            {
                return null;
            }

            var normalizedFragment = fragment.StartsWith("?", StringComparison.Ordinal) ? fragment : $"?{fragment}";
            var query = QueryHelpers.ParseQuery(normalizedFragment);
            if (query.TryGetValue("login_hint", out var loginHintValues))
            {
                return loginHintValues.FirstOrDefault();
            }

            return null;
        }
    }
}
