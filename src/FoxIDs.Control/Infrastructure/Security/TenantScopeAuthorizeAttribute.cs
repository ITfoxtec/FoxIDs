using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

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
                policy.Requirements.Add(new ScopeAndRoleAuthorizationRequirement(new List<ScopeAndRoleAuthorizationRequirement.ScopeAndRole>
                {
                   new ScopeAndRoleAuthorizationRequirement.ScopeAndRole { Scope = Constants.ControlApi.ResourceAndScope.Tenant },
                   new ScopeAndRoleAuthorizationRequirement.ScopeAndRole { Scope = Constants.ControlApi.ResourceAndScope.TenantUser, Role = Constants.ControlApi.Role.TenantAdmin },
                }));
            });
        }
    }
}
