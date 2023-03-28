using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using FoxIDs.Models.Config;
using StackExchange.Redis;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using FoxIDs.Logic;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsRouteBindingMiddleware : RouteBindingMiddleware
    {
        private static Regex partyNameBindingRegex = new Regex(@"^(?:(?:(?<downparty>[\w-_]+)(?:\((?:(?:(?<toupparty>[\w-_]+)(?:,(?<toupparty>[\w-_]+))*)|(?<toupparty>\*))\))?)|(?:(?<downparty>[\w-_]+)(?:\~(?:(?:(?<toupparty>[\w-_]+)(?:,(?<toupparty>[\w-_]+))*)|(?<toupparty>\*))\~)?)|(?:(?<downparty>[\w-_]+)(?:.(?:(?:(?<toupparty>[\w-_]+)(?:,(?<toupparty>[\w-_]+))*)|(?<toupparty>\*)).)?)|(?:\((?<upparty>[\w-_]+)\))|(?:\~(?<upparty>[\w-_]+)\~)|(?:.(?<upparty>[\w-_]+).))$", RegexOptions.Compiled);
        private readonly FoxIDsSettings settings;
        private readonly DownPartyCacheLogic downPartyCacheLogic;
        private readonly UpPartyCacheLogic upPartyCacheLogic;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly TokenCredential tokenCredential;

        public FoxIDsRouteBindingMiddleware(RequestDelegate next, FoxIDsSettings settings, TrackCacheLogic trackCacheLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, IConnectionMultiplexer redisConnectionMultiplexer, TokenCredential tokenCredential) : base(next, trackCacheLogic)
        {
            this.settings = settings;
            this.downPartyCacheLogic = downPartyCacheLogic;
            this.upPartyCacheLogic = upPartyCacheLogic;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tokenCredential = tokenCredential;
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

        protected override Track.IdKey GetTrackIdKey(string[] route, bool hasCustomDomain)
        {
            if (route.Length == 0)
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
            else if ((!hasCustomDomain && route.Length > 2) || (hasCustomDomain && route.Length > 1))
            {
                if (!hasCustomDomain)
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
            else if ((!hasCustomDomain && route.Length == 2) || (hasCustomDomain && route.Length == 1))
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

        protected override string GetPartyNameAndbinding(string[] route, bool hasCustomDomain)
        {
            if ((!hasCustomDomain && route.Length >= 3) || (hasCustomDomain && route.Length >= 2))
            {
                if (!hasCustomDomain)
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
            routeBinding.PartyNameAndBinding = partyNameAndBinding;
            routeBinding.Key = await LoadTrackKeyAsync(scopedLogger, trackIdKey, track);
            routeBinding.ClaimMappings = track.ClaimMappings;
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
                    throw new ArgumentException($"Invalid party name and binding match. PartyNameAndBinding '{partyNameAndBinding}'");
                }

                if (partyNameBindingMatch.Groups["upparty"].Success)
                {
                    routeBinding.UpParty = await GetUpPartyAsync(trackIdKey, partyNameBindingMatch.Groups["upparty"], acceptUnknownParty);
                }
                else if (partyNameBindingMatch.Groups["downparty"].Success)
                {
                    routeBinding.DownParty = await GetDownPartyAsync(trackIdKey, partyNameBindingMatch.Groups["downparty"], acceptUnknownParty);

                    if (routeBinding.DownParty?.AllowUpParties?.Count() >= Constants.Models.TrackLinkDownParty.SelectedUpPartiesMin)
                    {
                        //TODO can be deleted when data is updated everywhere, delete after summer 2023
                        var allowUpParties = routeBinding.DownParty.AllowUpParties.OrderBy(p => p.Type).ThenBy(p => p.Name);

                        if (partyNameBindingMatch.Groups["toupparty"].Success)
                        {
                            routeBinding.ToUpParties = GetAllowedToUpPartyIds(scopedLogger, partyNameBindingMatch.Groups["toupparty"], routeBinding.DownParty.Id, allowUpParties);
                        }
                        else
                        {
                            routeBinding.ToUpParties = routeBinding.DownParty.AllowUpParties;
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid party name and binding group match. PartyNameAndBinding '{partyNameAndBinding}'");
                }
            }

            return routeBinding;
        }

        private async Task<RouteTrackKey> LoadTrackKeyAsync(TelemetryScopedLogger scopedLogger, Track.IdKey trackIdKey, Track track)
        {
            switch (track.Key.Type)
            {
                case TrackKeyType.Contained:
                    return new RouteTrackKey
                    {
                        Type = track.Key.Type,
                        PrimaryKey = new RouteTrackKeyItem { Key = track.Key.Keys[0].Key },
                        SecondaryKey = track.Key.Keys.Count > 1 ? new RouteTrackKeyItem { Key = track.Key.Keys[1].Key } : null,
                    };

                case TrackKeyType.KeyVaultRenewSelfSigned:
                    var trackKeyExternal = await GetTrackKeyItemsAsync(scopedLogger, trackIdKey.TenantName, trackIdKey.TrackName, track);
                    var externalRouteTrackKey = new RouteTrackKey
                    {
                        Type = track.Key.Type,
                        ExternalName = track.Key.ExternalName,
                    };
                    if (trackKeyExternal == null)
                    {
                        externalRouteTrackKey.PrimaryKey = new RouteTrackKeyItem { ExternalKeyIsNotReady = true };
                    }
                    else
                    {
                        externalRouteTrackKey.PrimaryKey = new RouteTrackKeyItem { ExternalId = trackKeyExternal.Keys[0].ExternalId, Key = trackKeyExternal.Keys[0].Key };
                        externalRouteTrackKey.SecondaryKey = trackKeyExternal.Keys.Count > 1 ? new RouteTrackKeyItem { ExternalId = trackKeyExternal.Keys[1].ExternalId, Key = trackKeyExternal.Keys[1].Key } : null;
                    }
                    return externalRouteTrackKey;

                case TrackKeyType.KeyVaultUpload:
                default:
                    throw new Exception($"Track key type not supported '{track.Key.Type}'.");
            }
        }

        private async Task<UpParty> GetUpPartyAsync(Track.IdKey trackIdKey, Group upPartyGroup, bool acceptUnknownParty)
        {
            try
            {
                return await upPartyCacheLogic.GetUpPartyAsync(upPartyGroup.Value, tenantName: trackIdKey.TenantName, trackName: trackIdKey.TrackName, required: !acceptUnknownParty);
            }
            catch (Exception ex)
            {
                throw new RouteCreationException($"Invalid tenant '{trackIdKey.TenantName}', track '{trackIdKey.TrackName}' and up-party '{upPartyGroup.Value}' combination.", ex);
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
                throw new RouteCreationException($"Invalid tenant '{trackIdKey.TenantName}', track '{trackIdKey.TrackName}' and down-party '{downPartyGroup.Value}' combination.", ex);
            }
        }

        private List<UpPartyLink> GetAllowedToUpPartyIds(TelemetryScopedLogger scopedLogger, Group toUpPartyGroup, string downPartyId, IEnumerable<UpPartyLink> allowUpParties)
        {
            if (toUpPartyGroup.Captures.Count > Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax)
            {
                throw new ArgumentException($"More then {Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax} to up-party for down-party '{downPartyId}'.");
            }

            var toUpParties = new List<UpPartyLink>();
            foreach (Capture upPartyCapture in toUpPartyGroup.Captures)
            {
                if (upPartyCapture.Value == "*")
                {
                    toUpParties.Clear();
                    toUpParties.AddRange(allowUpParties);
                    break;
                }
                else
                {
                    var allowUpParty = allowUpParties.Where(ap => ap.Name.Equals(upPartyCapture.Value, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
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
                            throw new ArgumentException($"Up-party name '{upPartyCapture.Value}' not allowed for down-party '{downPartyId}',");
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

        public async Task<TrackKeyExternal> GetTrackKeyItemsAsync(TelemetryScopedLogger scopedLogger, string tenantName, string trackName, Track track)
        {
            var key = RadisTrackKeyExternalKey(tenantName, trackName, track.Key.ExternalName);
            var db = redisConnectionMultiplexer.GetDatabase();

            var trackKeyExternalValue = (string)await db.StringGetAsync(key);
            if (!trackKeyExternalValue.IsNullOrEmpty())
            {
                return trackKeyExternalValue.ToObject<TrackKeyExternal>();
            }

            var trackKeyExternal = await LoadTrackKeyExternalFromKeyVaultAsync(scopedLogger, track);
            if (trackKeyExternal != null)
            {
                await db.StringSetAsync(key, trackKeyExternal.ToJson(), TimeSpan.FromSeconds(track.KeyExternalCacheLifetime));
            }
            return trackKeyExternal;
        }

        private async Task<TrackKeyExternal> LoadTrackKeyExternalFromKeyVaultAsync(TelemetryScopedLogger scopedLogger, Track track)
        {
            var now = DateTimeOffset.UtcNow;
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);

            var externalKeys = new List<(string Id, DateTimeOffset? NotBefore)>();
            var certificateVersions = certificateClient.GetPropertiesOfCertificateVersionsAsync(track.Key.ExternalName);
            await foreach (var certificateVersion in certificateVersions)
            {
                if (certificateVersion.Enabled == true && certificateVersion.ExpiresOn >= now)
                {
                    externalKeys.Add((certificateVersion.Version, certificateVersion.NotBefore));
                }
            }

            if (externalKeys.Count <= 0)
            {
                try
                {
                    throw new Exception($"Track key external certificate '{track.Key.ExternalName}' do not exist in Key Vault, probably because it is not ready in Key Vault.");
                }
                catch (Exception ex)
                {
                    scopedLogger.Warning(ex);
                    return null;
                }            
            }

            var trackKeyExternal = new TrackKeyExternal();
            if (externalKeys.Count == 1)
            {
                trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = externalKeys.First().Id } };
            }
            else
            {
                var topTwoExternalKeys = externalKeys.OrderByDescending(e => e.NotBefore).Take(2);
                var firstExternalKey = topTwoExternalKeys.First();
                if (firstExternalKey.NotBefore <= now.AddDays(-track.KeyExternalPrimaryAfterDays))
                {
                    trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = firstExternalKey.Id }, new TrackKeyExternalItem { ExternalId = topTwoExternalKeys.Last().Id } };
                }
                else
                {
                    trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = topTwoExternalKeys.Last().Id }, new TrackKeyExternalItem { ExternalId = firstExternalKey.Id } };
                }
            }

            foreach (var keyItem in trackKeyExternal.Keys)
            {
                var certificateWithPolicy = certificateClient.GetCertificateVersion(track.Key.ExternalName, keyItem.ExternalId);
                var certificateRawValue = certificateWithPolicy?.Value?.Cer;
                if (certificateRawValue == null)
                {
                    throw new Exception($"Track key external certificate '{track.Key.ExternalName}' version '{keyItem.ExternalId}' from Key Vault is null.");
                }
                keyItem.Key = await new X509Certificate2(certificateRawValue).ToFTJsonWebKeyAsync();
            }
            return trackKeyExternal;
        }

        private string RadisTrackKeyExternalKey(string tenantName, string trackName, string name)
        {
            return $"track_key_ext_{tenantName}_{trackName}_{name}";
        }
    }
}
