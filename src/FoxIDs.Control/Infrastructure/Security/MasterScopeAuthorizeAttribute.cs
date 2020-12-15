using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using Microsoft.AspNetCore.Authorization;

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
                policy.RequireScopeAndRoles(
                    new ScopeAndRoles { Scope = Constants.ControlApi.ResourceAndScope.Master, Roles = new[] { Constants.ControlApi.Role.TenantAdmin } }
                );
            });
        }
    }
}
