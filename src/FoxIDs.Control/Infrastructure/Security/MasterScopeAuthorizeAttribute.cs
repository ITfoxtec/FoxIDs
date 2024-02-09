using Microsoft.AspNetCore.Authorization;

namespace FoxIDs.Infrastructure.Security
{
    public class MasterScopeAuthorizeAttribute : BaseScopeAuthorizeAttribute
    {
        public const string Name = nameof(MasterScopeAuthorizeAttribute);

        public MasterScopeAuthorizeAttribute(params string[] segments) : base(Name, segments)
        { }

        public static void AddPolicy(AuthorizationOptions options)
        {
            options.AddPolicy(Name, policy =>
            {
                policy.Requirements.Add(new MasterAuthorizationRequirement());
            });
        }
    }
}
