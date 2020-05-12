using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Threading.Tasks;

namespace FoxIDs.Security
{
    public class TenantAccessTokenMessageHandler : AccessTokenMessageHandler
    {
        private readonly TenantOpenidConnectPkce tenantOpenidConnectPkce;

        public TenantAccessTokenMessageHandler(NavigationManager navigationManager, OpenidConnectPkce openidConnectPkce, AuthenticationStateProvider authenticationStateProvider) : base(navigationManager, openidConnectPkce, authenticationStateProvider)
        {
            tenantOpenidConnectPkce = openidConnectPkce as TenantOpenidConnectPkce;
        }

        protected override async Task LoginAsync()
        {
            await tenantOpenidConnectPkce.TenantLoginAsync();
        }
    }
}
