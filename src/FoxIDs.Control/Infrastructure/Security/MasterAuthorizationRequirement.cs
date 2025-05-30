﻿using System;
using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Security
{
    public class MasterAuthorizationRequirement : BaseAuthorizationRequirement<MasterAuthorizationRequirement, MasterScopeAuthorizeAttribute>
    {
        protected override (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string trackName, string httpMethod, IEnumerable<string> accessToTrackNames = null)
        {
            if (accessToTrackNames != null)
            {
                throw new NotSupportedException("Access to track names not supported in master authorization.");
            }

            var acceptedScopes = new List<string>();
            var acceptedRoles = new List<string>();

            AddScopeAndRole(acceptedScopes, acceptedRoles, httpMethod, Constants.ControlApi.ResourceAndScope.Master, Constants.ControlApi.Access.Master);
            acceptedRoles.Add(Constants.ControlApi.Access.TenantAdminRole);

            foreach (var segment in segments)
            {
                if (segment == Constants.ControlApi.Segment.Basic || segment == Constants.ControlApi.Segment.AnyTrack)
                {
                    throw new NotSupportedException($"Segment {segment}' not supported in master authorization.");
                }

                var scope = $"{Constants.ControlApi.ResourceAndScope.Master}{segment}";
                var role = $"{Constants.ControlApi.Access.Tenant}{segment}";
                AddScopeAndRole(acceptedScopes, acceptedRoles, httpMethod, scope, role, segment);
            }

            return (acceptedScopes, acceptedRoles);
        }
    }
}
