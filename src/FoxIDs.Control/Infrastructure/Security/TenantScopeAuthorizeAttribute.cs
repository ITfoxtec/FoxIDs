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
                policy.Requirements.Add(new ScopeRoleAuthorizationRequirement { ScopeRoleList = new List<ScopeRoleAuthorizationRequirement.ScopeRole>
                {
                   new ScopeRoleAuthorizationRequirement.ScopeRole { Scope = Constants.ControlApi.Scope.Tenant },
                   new ScopeRoleAuthorizationRequirement.ScopeRole { Scope = Constants.ControlApi.Scope.TenantUser, Role = Constants.ControlApi.Role.TenantAdmin },
                }});
            });
        }
    }
}
