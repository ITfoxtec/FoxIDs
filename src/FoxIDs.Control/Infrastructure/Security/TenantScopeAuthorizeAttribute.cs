using Microsoft.AspNetCore.Authorization;
using static FoxIDs.Infrastructure.Security.ScopeAndRolesAuthorizationRequirement;

namespace FoxIDs.Infrastructure.Security
{
    public class TenantScopeAuthorizeAttribute : AuthorizeAttribute
    {
        public const string Name = nameof(TenantScopeAuthorizeAttribute);

        public TenantScopeAuthorizeAttribute() : base(Name)
        {
            AuthenticationSchemes = JwtBearerMultipleTenantsHandler.AuthenticationScheme;
        }

        public static void AddPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(Name, policy =>
            {
                policy.Requirements.Add(new ScopeAndRolesAuthorizationRequirement(new []
                {
                   new ScopeAndRoles { Scope = Constants.ControlApi.ResourceAndScope.Tenant },
                   new ScopeAndRoles { Scope = Constants.ControlApi.ResourceAndScope.TenantUser, Roles = new [] { Constants.ControlApi.Role.TenantAdmin } },
                }));
            });
        }
    }
}
