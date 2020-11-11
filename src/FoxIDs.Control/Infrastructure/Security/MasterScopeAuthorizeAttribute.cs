using Microsoft.AspNetCore.Authorization;
using static FoxIDs.Infrastructure.Security.ScopeAndRolesAuthorizationRequirement;

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
                policy.Requirements.Add(new ScopeAndRolesAuthorizationRequirement(new []
                {
                   new ScopeAndRoles { Scope = Constants.ControlApi.ResourceAndScope.Master },
                   new ScopeAndRoles { Scope = Constants.ControlApi.ResourceAndScope.MasterUser, Roles = new [] { Constants.ControlApi.Role.TenantAdmin } },
                }));
            });
        }
    }
}
