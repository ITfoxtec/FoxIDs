using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Security
{
    public class MasterAuthorizationRequirement : BaseAuthorizationRequirement<MasterAuthorizationRequirement, MasterScopeAuthorizeAttribute>
    {
        protected override (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string tenantName, string trackName, bool isHttpGet)
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
