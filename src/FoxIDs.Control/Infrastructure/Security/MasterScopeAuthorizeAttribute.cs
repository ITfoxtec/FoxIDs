using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Security
{
    public class MasterScopeAuthorizeAttribute : AuthorizeAttribute
    {
        public const string Name = nameof(MasterScopeAuthorizeAttribute);

        public MasterScopeAuthorizeAttribute() : base(Name)
        {
            AuthenticationSchemes = JwtBearerMultipleTenantsHandler.AuthenticationScheme;
        }

        public static void AddPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(Name, policy =>
            {
                policy.Requirements.Add(new ScopeAndRoleAuthorizationRequirement(new List<ScopeAndRoleAuthorizationRequirement.ScopeAndRole>
                {
                   new ScopeAndRoleAuthorizationRequirement.ScopeAndRole { Scope = Constants.ControlApi.ResourceAndScope.Master },
                   new ScopeAndRoleAuthorizationRequirement.ScopeAndRole { Scope = Constants.ControlApi.ResourceAndScope.MasterUser, Role = Constants.ControlApi.Role.TenantAdmin },
                }));
            });
        }
    }
}
