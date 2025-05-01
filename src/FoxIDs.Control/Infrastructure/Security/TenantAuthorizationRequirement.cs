using ITfoxtec.Identity;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Infrastructure.Security
{
    public class TenantAuthorizationRequirement : BaseAuthorizationRequirement<TenantAuthorizationRequirement, TenantScopeAuthorizeAttribute>
    {
        public TenantAuthorizationRequirement()
        {
            supportAccessToTracks = true;       
        }

        protected override (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string trackName, string httpMethod, IEnumerable<string> accessToTrackNames = null)
        {
            var acceptedScopes = new List<string>();
            var acceptedRoles = new List<string>();

            AddScopeAndRole(acceptedScopes, acceptedRoles, httpMethod, Constants.ControlApi.ResourceAndScope.Tenant, Constants.ControlApi.Access.Tenant);
            acceptedRoles.Add(Constants.ControlApi.Access.TenantAdminRole);

            if (!trackName.IsNullOrWhiteSpace())
            {
                AddScopeAndRoleByTrack(acceptedScopes, acceptedRoles, trackName, httpMethod,
                    $"{Constants.ControlApi.ResourceAndScope.Tenant}{Constants.ControlApi.AccessElement.Track}",
                    $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}");

                if (segments?.Count() > 0)
                {
                    foreach (var segment in segments)
                    {
                        if (segment == Constants.ControlApi.Segment.Basic)
                        {
                            AddScopeAndRole(acceptedScopes, acceptedRoles, httpMethod,
                                $"{Constants.ControlApi.ResourceAndScope.Tenant}{segment}",
                                $"{Constants.ControlApi.Access.Tenant}{segment}");
                        }
                        else if (segment == Constants.ControlApi.Segment.AnyTrack)
                        {
                            AddScopeAndRoleAnyTrack(acceptedScopes, acceptedRoles, accessToTrackNames, httpMethod,
                                $"{Constants.ControlApi.ResourceAndScope.Tenant}{Constants.ControlApi.AccessElement.Track}",
                                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}");
                        }
                        else
                        {
                            AddScopeAndRoleByTrack(acceptedScopes, acceptedRoles, trackName, httpMethod, 
                                $"{Constants.ControlApi.ResourceAndScope.Tenant}{Constants.ControlApi.AccessElement.Track}",
                                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}",
                                segment);
                        }       
                    }
                }
            }

            return (acceptedScopes, acceptedRoles);
        }

        private void AddScopeAndRoleByTrack(List<string> acceptedScopes, List<string> acceptedRoles, string trackName, string httpMethod, string subScope, string subRole, string segment = "")
        {
            if (trackName != Constants.Routes.MasterTrackName)
            {
                AddScopeAndRole(acceptedScopes, acceptedRoles, httpMethod, $"{subScope}{segment}", $"{subRole}{segment}", segment);
            }

            AddScopeAndRole(acceptedScopes, acceptedRoles, httpMethod, $"{subScope}[{trackName}]{segment}", $"{subRole}[{trackName}]{segment}", segment);
        }

        private void AddScopeAndRoleAnyTrack(List<string> acceptedScopes, List<string> acceptedRoles, IEnumerable<string> accessToTrackNames, string httpMethod, string subScope, string subRole)
        {
            if (accessToTrackNames?.Count() > 0)
            {
                foreach (string accessToTrackName in accessToTrackNames)
                {
                    AddScopeAndRole(acceptedScopes, acceptedRoles, httpMethod, $"{subScope}[{accessToTrackName}]", $"{subRole}[{accessToTrackName}]");
                }
            }
        }
    }
}
