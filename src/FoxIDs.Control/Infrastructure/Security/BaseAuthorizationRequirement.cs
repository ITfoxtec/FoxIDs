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
                        (var acceptedScopes, var acceptedRoles) = GetAcceptedScopesAndRoles(scopeAuthorizeAttribute.Segments, httpContext.GetRouteBinding()?.TrackName, httpContext.Request?.Method);

                        if (userScopes.Where(us => acceptedScopes.Any(s => s.Equals(us, StringComparison.Ordinal))).Any() && userRoles.Where(ur => acceptedRoles.Any(r => r.Equals(ur, StringComparison.Ordinal))).Any())
                        {
                            context.Succeed(requirement);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        protected abstract (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string trackName, string httpMethod);

        protected void AddScopeAndRole(List<string> acceptedScopes, List<string> acceptedRoles, string httpMethod, string scope, string role, string segment = "")
        {
            acceptedScopes.Add(scope);
            acceptedRoles.Add(role);

            if (segment != Constants.ControlApi.Segment.Usage)
            {
                if (httpMethod == HttpMethod.Get.Method)
                {
                    acceptedScopes.Add($"{scope}{Constants.ControlApi.AccessElement.Read}");
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Read}");
                }
                else if (httpMethod == HttpMethod.Post.Method)
                {
                    acceptedScopes.Add($"{scope}{Constants.ControlApi.AccessElement.Create}");
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Create}");

                }
                else if (httpMethod == HttpMethod.Put.Method)
                {
                    acceptedScopes.Add($"{scope}{Constants.ControlApi.AccessElement.Update}");
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Update}");

                }
                else if (httpMethod == HttpMethod.Delete.Method)
                {
                    acceptedScopes.Add($"{scope}{Constants.ControlApi.AccessElement.Delete}");
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Delete}");
                }
            }
        }
    }
}
