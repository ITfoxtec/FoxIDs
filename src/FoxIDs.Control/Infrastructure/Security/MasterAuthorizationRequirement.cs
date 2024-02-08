using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Security
{
    public class MasterAuthorizationRequirement : BaseAuthorizationRequirement<MasterAuthorizationRequirement, MasterScopeAuthorizeAttribute>
    {
        protected override (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string tenantName, string trackName, bool isHttpGet)
        {
            return GetMasterAcceptedScopesAndRoles(segments, isHttpGet);
        }
    }
}
