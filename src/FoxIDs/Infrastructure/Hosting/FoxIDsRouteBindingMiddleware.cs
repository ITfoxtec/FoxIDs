using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FoxIDs.Logic;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsRouteBindingMiddleware : RouteBindingMiddleware
    {
        private static Regex partyNameBindingRegex = new Regex(@"^(?:(?:(?<downparty>[\w-]+)(?:\((?:(?:(?<toupparty>[\w+-]+)(?:,(?<toupparty>[\w+-]+))*)|(?<toupparty>\*))\))?)|(?:(?<downparty>[\w-]+)(?:\~(?:(?:(?<toupparty>[\w+-]+)(?:,(?<toupparty>[\w+-]+))*)|(?<toupparty>\*))\~)?)|(?:(?<downparty>[\w-]+)(?:.(?:(?:(?<toupparty>[\w+-]+)(?:,(?<toupparty>[\w+-]+))*)|(?<toupparty>\*)).)?)|(?:\((?<upparty>[\w-]+)\))|(?:\~(?<upparty>[\w-]+)\~)|(?:.(?<upparty>[\w-]+).))$", RegexOptions.Compiled);
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly DownPartyCacheLogic downPartyCacheLogic;
        private readonly UpPartyCacheLogic upPartyCacheLogic;

        public FoxIDsRouteBindingMiddleware(RequestDelegate next, TrackKeyLogic trackKeyLogic, TrackCacheLogic trackCacheLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic) : base(next, trackCacheLogic)
        {
            this.trackKeyLogic = trackKeyLogic;
            this.downPartyCacheLogic = downPartyCacheLogic;
            this.upPartyCacheLogic = upPartyCacheLogic;
        }

        protected override bool CheckCustomDomainSupport(string[] route)
        {
            if (route.Length > 1)
            {
                if(route[0].Equals(Constants.Routes.MasterTrackName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (route.Length > 2)
                {
                    if (route[1].Equals(Constants.Routes.MasterTrackName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected override Track.IdKey GetTrackIdKey(string[] route, bool useCustomDomain)
        {
            if (route.Length == 0)
            {
                return null;
            }
            else if (route.Length >= 1 && route[0].Equals(Constants.Routes.HealthController, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            else if (route.Length >= 1 && route[0].Equals(Constants.Routes.DefaultSiteController, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            else if (route.Length >= 1 && route[0].Equals(Constants.Routes.ErrorController, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            else if ((!useCustomDomain && route.Length > 2) || (useCustomDomain && route.Length > 1))
            {
                if (!useCustomDomain)
                {
                    return new Track.IdKey
                    {
                        TenantName = route[0].ToLower(),
                        TrackName = route[1].ToLower()
                    };
                }
                else
                {
                    return new Track.IdKey
                    {
                        TrackName = route[0].ToLower()
                    };
                }
            }
            else if ((!useCustomDomain && route.Length == 2) || (useCustomDomain && route.Length == 1))
            {
                throw new NotSupportedException($"FoxIDs route '{string.Join('/', route)}' without an action is not supported.");
            }
            else
            {
                throw new NotSupportedException($"FoxIDs route '{string.Join('/', route)}' is not supported.");
            }
        }

        protected override bool AcceptUnknownParty(string path, string[] route)
        {
            if(route.Length > 2)
            {
                if (path.EndsWith(IdentityConstants.OidcDiscovery.Path, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
                else if (path.EndsWith(IdentityConstants.OidcDiscovery.Keys, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
                else if (path.EndsWith($"{Constants.Routes.SamlController}/{Constants.Endpoints.SamlIdPMetadata}", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
                else if (path.EndsWith($"{Constants.Routes.SamlController}/{Constants.Endpoints.SamlSPMetadata}", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        protected override string GetPartyNameAndbinding(string[] route, bool useCustomDomain)
        {
            if ((!useCustomDomain && route.Length >= 3) || (useCustomDomain && route.Length >= 2))
            {
                if (!useCustomDomain)
                {
                    return route[2].ToLower();
                }
                else
                {
                    return route[1].ToLower();
                }
            }
            else
            {
                return null;
            }
        }

        protected override async ValueTask<RouteBinding> PostRouteDataAsync(TelemetryScopedLogger scopedLogger, IServiceProvider requestServices, Track.IdKey trackIdKey, Track track, RouteBinding routeBinding, string partyNameAndBinding, bool acceptUnknownParty)
        {
            routeBinding.DisplayName = track.DisplayName;
            routeBinding.CompanyName = track.CompanyName;
            routeBinding.PartyNameAndBinding = partyNameAndBinding;
            routeBinding.Key = await trackKeyLogic.LoadTrackKeyAsync(scopedLogger, trackIdKey, track);
            routeBinding.ClaimMappings = track.ClaimMappings;
            routeBinding.AutoMapSamlClaims = track.AutoMapSamlClaims;
            routeBinding.SequenceLifetime = track.SequenceLifetime;
            routeBinding.MaxFailingLogins = track.MaxFailingLogins;
            routeBinding.FailingLoginCountLifetime = track.FailingLoginCountLifetime;
            routeBinding.FailingLoginObservationPeriod = track.FailingLoginObservationPeriod;
            routeBinding.PasswordLength = track.PasswordLength;
            routeBinding.CheckPasswordComplexity = track.CheckPasswordComplexity.Value;
            routeBinding.CheckPasswordRisk = track.CheckPasswordRisk.Value;
            routeBinding.AllowIframeOnDomains = track.AllowIframeOnDomains;
            routeBinding.SendEmail = track.SendEmail;
            routeBinding.Logging = track.Logging;

            if (!partyNameAndBinding.IsNullOrWhiteSpace())
            {
                var partyNameBindingMatch = partyNameBindingRegex.Match(partyNameAndBinding);
                if (!partyNameBindingMatch.Success)
                {
                    throw new ArgumentException($"Invalid connection name and binding match. PartyNameAndBinding '{partyNameAndBinding}'");
                }

                if (partyNameBindingMatch.Groups["upparty"].Success)
                {
                    routeBinding.UpParty = await GetUpPartyAsync(trackIdKey, partyNameBindingMatch.Groups["upparty"], acceptUnknownParty);
                }
                else if (partyNameBindingMatch.Groups["downparty"].Success)
                {
                    routeBinding.DownParty = await GetDownPartyAsync(trackIdKey, partyNameBindingMatch.Groups["downparty"], acceptUnknownParty);

                    var allowUpParties = routeBinding.DownParty?.AllowUpParties?.Where(up => !up.DisableUserAuthenticationTrust)?.ToList();
                    if (allowUpParties?.Count() >= Constants.Models.TrackLinkDownParty.SelectedUpPartiesMin)
                    {
                        if (partyNameBindingMatch.Groups["toupparty"].Success)
                        {
                            routeBinding.ToUpParties = GetAllowedToUpPartyIds(scopedLogger, partyNameBindingMatch.Groups["toupparty"], routeBinding.DownParty.Id, allowUpParties);
                        }
                        else
                        {
                            routeBinding.ToUpParties = allowUpParties;
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid connection name and binding group match. PartyNameAndBinding '{partyNameAndBinding}'");
                }
            }

            return routeBinding;
        }

        private async Task<UpPartyWithProfile<UpPartyProfile>> GetUpPartyAsync(Track.IdKey trackIdKey, Group upPartyGroup, bool acceptUnknownParty)
        {
            try
            {
                return await upPartyCacheLogic.GetUpPartyAsync(upPartyGroup.Value, tenantName: trackIdKey.TenantName, trackName: trackIdKey.TrackName, required: !acceptUnknownParty);
            }
            catch (Exception ex)
            {
                throw new RouteCreationException($"Invalid tenant '{trackIdKey.TenantName}', environment '{trackIdKey.TrackName}' and authentication method '{upPartyGroup.Value}' combination.", ex);
            }
        }

        private async Task<DownParty> GetDownPartyAsync(Track.IdKey trackIdKey, Group downPartyGroup, bool acceptUnknownParty)
        {
            try
            {
                return await downPartyCacheLogic.GetDownPartyAsync(downPartyGroup.Value, tenantName: trackIdKey.TenantName, trackName: trackIdKey.TrackName, required: !acceptUnknownParty);
            }
            catch (Exception ex)
            {
                throw new RouteCreationException($"Invalid tenant '{trackIdKey.TenantName}', environment '{trackIdKey.TrackName}' and application registration '{downPartyGroup.Value}' combination.", ex);
            }
        }

        private List<UpPartyLink> GetAllowedToUpPartyIds(TelemetryScopedLogger scopedLogger, Group toUpPartyGroup, string downPartyId, IEnumerable<UpPartyLink> allowUpParties)
        {
            if (toUpPartyGroup.Captures.Count > Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax)
            {
                throw new ArgumentException($"More then {Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax} to authentication method for application registration '{downPartyId}'.");
            }

            var toUpParties = new List<UpPartyLink>();
            foreach (Capture upPartyAndProfileCapture in toUpPartyGroup.Captures)
            {
                var upPartyAndProfileSplit = upPartyAndProfileCapture.Value.Split('+');
                var upPartyCapture = upPartyAndProfileSplit[0];
                var profileCapture = upPartyAndProfileSplit.Length == 2 ? upPartyAndProfileSplit[1] : null;

                if (upPartyCapture == "*")
                {
                    toUpParties.Clear();
                    toUpParties.AddRange(allowUpParties);
                    break;
                }
                else
                {
                    var allowUpParty = allowUpParties.Where(ap => ap.Name.Equals(upPartyCapture, StringComparison.OrdinalIgnoreCase) && 
                        (profileCapture.IsNullOrEmpty() && ap.ProfileName.IsNullOrEmpty() || ap.ProfileName?.Equals(profileCapture, StringComparison.OrdinalIgnoreCase) == true)).FirstOrDefault();
                    if (allowUpParty != null)
                    {
                        if (!toUpParties.Contains(allowUpParty))
                        {
                            toUpParties.Add(allowUpParty);
                        }
                    }
                    else
                    {
                        try
                        {
                            throw new ArgumentException($"Authentication method name '{upPartyCapture}'{(profileCapture.IsNullOrEmpty() ? string.Empty : $" and profile name '{profileCapture}'")} not allowed for application registration '{downPartyId}',");
                        }
                        catch (Exception ex)
                        {
                            scopedLogger.Warning(ex);
                        }
                    }
                }
            }

            return toUpParties;
        }
    }
}
