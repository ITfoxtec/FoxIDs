using FoxIDs.Logic;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using FoxIDs.Models.Logic;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsApiRouteBindingMiddleware : RouteBindingMiddleware
    {
        public FoxIDsApiRouteBindingMiddleware(RequestDelegate next, TrackCacheLogic trackCacheLogic) : base(next, trackCacheLogic)
        { }

        protected override bool CheckCustomDomainSupport(string[] route)
        {
            throw new NotSupportedException("Host in header not supported in Control API.");
        }

        protected override Track.IdKey GetTrackIdKey(string[] route, bool useCustomDomain)
        {
            if (route.Length >= 1 && route[0].Equals(Constants.Routes.HealthController, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            else
            {
                var trackIdKey = new Track.IdKey();
                trackIdKey.TrackName = Constants.Routes.MasterTrackName;

                if (route.Length >= 2 && route[0].Equals(Constants.Routes.MasterApiName, StringComparison.InvariantCultureIgnoreCase) && route[1].StartsWith(Constants.Routes.PreApikey, StringComparison.InvariantCulture))
                {
                    trackIdKey.TenantName = Constants.Routes.MasterTenantName;
                }
                else if (route.Length >= 2 && route[1].StartsWith(Constants.Routes.PreApikey, StringComparison.InvariantCulture))
                {
                    trackIdKey.TenantName = route[0].ToLower();
                }
                else if (route.Length >= 3 && route[2].StartsWith(Constants.Routes.PreApikey, StringComparison.InvariantCulture))
                {
                    trackIdKey.TenantName = route[0].ToLower();
                    trackIdKey.TrackName = route[1].ToLower();
                }
                else
                {
                    throw new NotSupportedException($"FoxIDs API route '{string.Join('/', route)}' not supported.");
                }

                return trackIdKey;
            }
        }

        protected override ValueTask<RouteBinding> PostRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, Track track, RouteBinding routeBinding, string partyNameAndBinding, bool acceptUnknownParty)
        {
            routeBinding.PasswordLength = track.PasswordLength;
            routeBinding.PasswordMaxLength = track.PasswordMaxLength.Value;
            routeBinding.CheckPasswordComplexity = track.CheckPasswordComplexity.Value;
            routeBinding.CheckPasswordRisk = track.CheckPasswordRisk.Value;
            routeBinding.PasswordBannedCharacters = track.PasswordBannedCharacters;
            routeBinding.PasswordHistory = track.PasswordHistory;
            routeBinding.PasswordMaxAge = track.PasswordMaxAge;
            routeBinding.SoftPasswordChange = track.SoftPasswordChange;
            routeBinding.PasswordPolicies = GetPasswordPolicies(track);
            routeBinding.Logging = track.Logging;

            return new ValueTask<RouteBinding>(routeBinding);
        }

        private List<PasswordPolicyState> GetPasswordPolicies(Track track)
        {
            if (track.PasswordPolicies == null || !(track.PasswordPolicies.Count > 0))
            {
                return null;
            }

            return track.PasswordPolicies.Select(p =>
                new PasswordPolicyState
                {
                    Name = p.Name,
                    MinLength = p.MinLength,
                    MaxLength = p.MaxLength,
                    CheckComplexity = p.CheckComplexity,
                    CheckRisk = p.CheckRisk,
                    BannedCharacters = p.BannedCharacters,
                    History = p.History,
                    MaxAge = p.MaxAge,
                    SoftChange = p.SoftChange
                }
            ).ToList();
        }
    }
}
