using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Security
{
    public class MasterAuthorizationRequirement : BaseAuthorizationRequirement<MasterAuthorizationRequirement, MasterScopeAuthorizeAttribute>
    {
        protected override (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string trackName, bool isHttpGet)
        {
            var acceptedScopes = new List<string>();
            var acceptedRoles = new List<string>();

            AddScopeAndRole(acceptedScopes, acceptedRoles, isHttpGet, Constants.ControlApi.ResourceAndScope.Master, Constants.ControlApi.Access.Master);
            acceptedRoles.Add(Constants.ControlApi.Access.TenantAdminRole);

            foreach (var segment in segments)
            {
                var scope = $"{Constants.ControlApi.ResourceAndScope.Master}{segment}";
                var role = $"{Constants.ControlApi.Access.Tenant}{segment}";
                AddScopeAndRole(acceptedScopes, acceptedRoles, isHttpGet, scope, role, segment);
            }

            return (acceptedScopes, acceptedRoles);
        }
    }
}
