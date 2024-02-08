using ITfoxtec.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Security
{
    public abstract class BaseAuthorizationRequirement<Tar, Tsc> : AuthorizationHandler<Tar>, IAuthorizationRequirement where Tar : IAuthorizationRequirement where Tsc : BaseScopeAuthorizeAttribute
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Tar requirement)
        {
            if (context.User != null && context.Resource is HttpContext httpContext)
            {
                var executingEnpoint = httpContext.GetEndpoint();
                var scopeAuthorizeAttribute = executingEnpoint.Metadata.OfType<Tsc>().FirstOrDefault();
                if (scopeAuthorizeAttribute != null)
                {
                    var userScopes = context.User.Claims.Where(c => string.Equals(c.Type, JwtClaimTypes.Scope, StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).FirstOrDefault().ToSpaceList();
                    var userRoles = context.User.Claims.Where(c => string.Equals(c.Type, JwtClaimTypes.Role, StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).ToList();
                    if (userScopes?.Count() > 0 && userRoles?.Count > 0)
                    {
                        var routeBinding = httpContext.GetRouteBinding();
                        (var acceptedScopes, var acceptedRoles) = GetAcceptedScopesAndRoles(scopeAuthorizeAttribute.Segments, routeBinding?.TenantName, routeBinding?.TrackName, httpContext.Request?.Method == HttpMethod.Get.Method);

                        if (userScopes.Where(us => acceptedScopes.Any(s => s.Equals(us, StringComparison.Ordinal))).Any() && userRoles.Where(ur => acceptedRoles.Any(r => r.Equals(ur, StringComparison.Ordinal))).Any())
                        {
                            context.Succeed(requirement);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        protected abstract (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string tenantName, string trackName, bool isHttpGet);

        protected (List<string> acceptedScopes, List<string> acceptedRoles) GetMasterAcceptedScopesAndRoles(IEnumerable<string> segments, bool isHttpGet)
        {
            var acceptedScopes = new List<string>();
            var acceptedRoles = new List<string>();

            acceptedScopes.Add(Constants.ControlApi.ResourceAndScope.Master);
            acceptedRoles.Add(Constants.ControlApi.Role.TenantAdmin);

            foreach (var segment in segments)
            {
                acceptedScopes.Add($"{Constants.ControlApi.ResourceAndScope.Master}{segment}");
                var role = $"{Constants.ControlApi.Role.Tenant}{segment}";
                acceptedRoles.Add(role);
                if (isHttpGet)
                {
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.ReadRole}");
                }
            }

            return (acceptedScopes, acceptedRoles);
        }
    }
}
