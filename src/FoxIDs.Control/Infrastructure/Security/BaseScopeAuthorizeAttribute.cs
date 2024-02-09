using Microsoft.AspNetCore.Authorization;

namespace FoxIDs.Infrastructure.Security
{
    public abstract class BaseScopeAuthorizeAttribute : AuthorizeAttribute
    {
        public BaseScopeAuthorizeAttribute(string name, params string[] subSegments) : base(name)
        {
            AuthenticationSchemes = JwtBearerMultipleTenantsHandler.AuthenticationScheme;
            Segments = subSegments;
        }

        public string[] Segments { get; }
    }
}
