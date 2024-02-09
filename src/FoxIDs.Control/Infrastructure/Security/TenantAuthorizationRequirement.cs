using ITfoxtec.Identity;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Infrastructure.Security
{
    public class TenantAuthorizationRequirement : BaseAuthorizationRequirement<TenantAuthorizationRequirement, TenantScopeAuthorizeAttribute>
    {
        protected override (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string trackName, bool isHttpGet)
        {
            var acceptedScopes = new List<string>();
            var acceptedRoles = new List<string>();

            AddScopeAndRole(acceptedScopes, acceptedRoles, isHttpGet, Constants.ControlApi.ResourceAndScope.Tenant, Constants.ControlApi.Access.Tenant);
            acceptedRoles.Add(Constants.ControlApi.Access.TenantAdminRole);

            if (!trackName.IsNullOrWhiteSpace())
            {
                if (segments?.Count() > 0)
                {
                    foreach (var segment in segments)
                    {
                        if (segment == Constants.ControlApi.Segment.Base)
                        {
                            AddScopeAndRole(acceptedScopes, acceptedRoles, isHttpGet,
                                $"{Constants.ControlApi.ResourceAndScope.Tenant}{segment}",
                                $"{Constants.ControlApi.Access.Tenant}{segment}");
                        }
                        else
                        {
                            AddScopeAndRoleByTrack(acceptedScopes, acceptedRoles, trackName, isHttpGet, 
                                    $"{Constants.ControlApi.ResourceAndScope.Tenant}{Constants.ControlApi.AccessElement.Track}", 
                                    Constants.ControlApi.Access.Tenant,
                                    segment);
                        }       
                    }
                }
                else
                {
                    AddScopeAndRoleByTrack(acceptedScopes, acceptedRoles, trackName, isHttpGet, 
                        $"{Constants.ControlApi.ResourceAndScope.Tenant}{Constants.ControlApi.AccessElement.Track}", 
                        Constants.ControlApi.Access.Tenant);
                }
            }

            return (acceptedScopes, acceptedRoles);
        }

        private void AddScopeAndRoleByTrack(List<string> acceptedScopes, List<string> acceptedRoles, string trackName, bool isHttpGet, string subScope, string subRole, string segment = "")
        {
            if (trackName != Constants.Routes.MasterTrackName)
            {
                AddScopeAndRole(acceptedScopes, acceptedRoles, isHttpGet, $"{subScope}{segment}", $"{subRole}{segment}", segment);
            }

            AddScopeAndRole(acceptedScopes, acceptedRoles, isHttpGet, $"{subScope}[{trackName}]{segment}", $"{subRole}[{trackName}]{segment}", segment);
        }
    }
}
