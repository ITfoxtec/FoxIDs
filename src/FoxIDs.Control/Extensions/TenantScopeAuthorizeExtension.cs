using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs
{
    public static class TenantScopeAuthorizeExtension
    {
        public static bool GetTenantScopeAccessToAnyTrack(this HttpContext httpContext)
        {
            return httpContext.Items.ContainsKey(Constants.ControlApi.AccessToAnyTrackKey);
        }

        public static IEnumerable<string> GetTenantScopeAccessToTrackNames(this HttpContext httpContext)
        {
            if (httpContext.Items.ContainsKey(Constants.ControlApi.AccessToTrackNamesKey))
            {
                return httpContext.Items[Constants.ControlApi.AccessToTrackNamesKey] as IEnumerable<string>;
            }
            return null;
        }

        public static void TenantScopeGrantAccessToTrackName(this HttpContext httpContext, string trackName)
        {
            if (httpContext.GetTenantScopeAccessToAnyTrack())
            {
                return;
            }

            var accessToTrackNames = httpContext.GetTenantScopeAccessToTrackNames();
            if (accessToTrackNames?.Count() > 0)
            {
                if (accessToTrackNames.Any(at => at == trackName))
                {
                    return;
                }

            }

            throw new UnauthorizedAccessException($"Users scope and role do not grant access to the {trackName} track.");
        }
    }
}
