using ITfoxtec.Identity;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Security
{
    public class TenantAuthorizationRequirement : BaseAuthorizationRequirement<TenantAuthorizationRequirement, TenantScopeAuthorizeAttribute>
    {
        protected override (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string tenantName, string trackName, bool isHttpGet)
        {
            if (tenantName == Constants.Routes.MasterTenantName && trackName == Constants.Routes.MasterTrackName)
            {
                return GetMasterAcceptedScopesAndRoles(segments, isHttpGet);
            }

            var acceptedScopes = new List<string>();
            var acceptedRoles = new List<string>();

            acceptedScopes.Add(Constants.ControlApi.ResourceAndScope.Tenant);
            acceptedRoles.Add(Constants.ControlApi.Role.TenantAdmin);

            foreach (var segment in segments)
            {
                var subScope = $"{Constants.ControlApi.ResourceAndScope.Tenant}{Constants.ControlApi.AccessElement.Track}";
                acceptedScopes.Add($"{subScope}{segment}");
                if (!trackName.IsNullOrWhiteSpace())
                {
                    acceptedScopes.Add($"{subScope}[{trackName}]{segment}");
                }

                var subRole = $"{Constants.ControlApi.Role.Tenant}";
                AddRole(acceptedRoles, subRole, segment, isHttpGet);
                if (!trackName.IsNullOrWhiteSpace())
                {
                    AddRole(acceptedRoles, $"{subScope}[{trackName}]", segment, isHttpGet);
                }
            }

            return (acceptedScopes, acceptedRoles);
        }

        private static void AddRole(List<string> acceptedRoles, string subRole, string segment, bool isHttpGet)
        {
            var role = $"{subRole}{segment}";
            acceptedRoles.Add(role);
            if (isHttpGet)
            {
                acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Read}");
            }
        }
    }
}
