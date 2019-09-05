using ITfoxtec.Identity;
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
                policy.RequireScope("foxids_api:foxids_master");
            });
        }
    }
}
