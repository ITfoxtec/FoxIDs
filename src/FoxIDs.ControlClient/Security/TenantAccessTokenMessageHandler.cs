using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Threading.Tasks;

namespace FoxIDs.Security
{
    public class TenantAccessTokenMessageHandler : AccessTokenMessageHandler
    {
        private readonly TenantOpenidConnectPkce tenantOpenidConnectPkce;

        public TenantAccessTokenMessageHandler(NavigationManager NavigationManager, OpenidConnectPkce OpenidConnectPkce, AuthenticationStateProvider authenticationStateProvider) : base(NavigationManager, OpenidConnectPkce, authenticationStateProvider)
        {
            tenantOpenidConnectPkce = OpenidConnectPkce as TenantOpenidConnectPkce;
        }

        protected override async Task LoginAsync()
        {
            await tenantOpenidConnectPkce.TenantLoginAsync();
        }
    }
}
