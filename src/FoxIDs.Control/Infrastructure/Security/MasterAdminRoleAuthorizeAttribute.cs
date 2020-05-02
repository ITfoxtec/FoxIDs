using Microsoft.AspNetCore.Authorization;

namespace FoxIDs.Infrastructure.Security
{
    public class MasterAdminRoleAuthorizeAttribute : AuthorizeAttribute
    {
        public const string Name = nameof(MasterAdminRoleAuthorizeAttribute);

        public MasterAdminRoleAuthorizeAttribute() : base(Name)
        {
            AuthenticationSchemes = JwtBearerMultipleTenantsHandler.AuthenticationScheme;
        }

        public static void AddPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(Name, policy =>
            {
                policy.RequireRole("foxids_master_admin");
            });
        }
    }
}
