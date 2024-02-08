using Microsoft.AspNetCore.Authorization;

namespace FoxIDs.Infrastructure.Security
{
    public class TenantScopeAuthorizeAttribute : BaseScopeAuthorizeAttribute
    {
        public const string Name = nameof(TenantScopeAuthorizeAttribute);

        public TenantScopeAuthorizeAttribute(params string[] segments) : base(Name, segments)
        { }

        public static void AddPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(Name, policy =>
            {
                policy.Requirements.Add(new TenantAuthorizationRequirement());
            });
        }
    }
}
