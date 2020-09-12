using ITfoxtec.Identity;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Security
{
    public class ScopeRoleAuthorizationRequirement : AuthorizationHandler<ScopeRoleAuthorizationRequirement>, IAuthorizationRequirement
    {
        /// <summary>
        /// List of scope and role values. One or more scope and role links must match.
        /// </summary>
        public List<ScopeRole> ScopeRoleList { get; set; }

        /// <summary>
        /// Makes a decision if authorization is allowed based on the scope and role list requirements specified.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <param name="requirement">The requirement to evaluate.</param>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRoleAuthorizationRequirement requirement)
        {
            if (context.User != null)
            {
                var scopeClaimValue = context.User.Claims.Where(c => string.Equals(c.Type, JwtClaimTypes.Scope, StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).FirstOrDefault();
                if (!scopeClaimValue.IsNullOrWhiteSpace())
                {
                    var scopes = scopeClaimValue.ToSpaceList();
                    foreach (var scope in scopes)
                    {
                        var scopeRoleItem = ScopeRoleList?.Where(sr => scope.Equals(sr.Scope, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
                        if(scopeRoleItem != null)
                        {
                            if(scopeRoleItem.Role.IsNullOrWhiteSpace())
                            {
                                context.Succeed(requirement);
                                break;
                            }
                            else
                            {
                                if(context.User.Claims.Where(c => string.Equals(c.Type, JwtClaimTypes.Role, StringComparison.OrdinalIgnoreCase) && scopeRoleItem.Role.Equals(c.Value, StringComparison.OrdinalIgnoreCase)).Any())
                                {
                                    context.Succeed(requirement);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }

        public class ScopeRole
        {
            public string Scope { get; set; }
            public string Role { get; set; }
        }
    }
}
