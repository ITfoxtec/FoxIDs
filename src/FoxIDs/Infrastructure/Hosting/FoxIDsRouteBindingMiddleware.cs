using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsRouteBindingMiddleware : RouteBindingMiddleware
    {
        private static Regex partyNameBindingRegex = new Regex(@"^(?:(?:(?<downparty>[\w-_]+)(?:\((?:(?:(?<toupparty>[\w-_]+)(?:,(?<toupparty>[\w-_]+))*)|(?<toupparty>\*))\))?)|(?:\((?<upparty>[\w-_]+)\)))$", RegexOptions.Compiled);
        private readonly ITenantRepository tenantRepository;

        public FoxIDsRouteBindingMiddleware(RequestDelegate next, ITenantRepository tenantRepository) : base(next, tenantRepository)
        {
            this.tenantRepository = tenantRepository;
        }

        protected override ValueTask SeedAsync(IServiceProvider requestServices) => default;

        protected override ValueTask<bool> PreAsync(HttpContext httpContext, string[] route) => new ValueTask<bool>(true);

        protected override Track.IdKey GetTrackIdKey(string[] route)
        {
            if (route.Length == 0)
            {
                return null;
            }
            else if (route.Length >= 1 && route[0].Equals(Constants.Routes.DefaultSiteController, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            else if (route.Length >= 2)
            {
                return new Track.IdKey
                {
                    TenantName = route[0].ToLower(),
                    TrackName = route[1].ToLower()
                };
            }
            else
            {
                throw new NotSupportedException($"FoxIDs route '{string.Join('/', route)}' not supported.");
            }
        }

        protected override string GetPartyNameAndbinding(string[] route)
        {
            if (route.Length >= 3)
            {
                return route[2].ToLower();
            }
            else
            {
                return null;
            }
        }

        protected override async ValueTask<RouteBinding> PostRouteDataAsync(TelemetryScopedLogger scopedLogger, Track.IdKey trackIdKey, Track track, RouteBinding routeBinding, string partyNameAndBinding = null)
        {
            routeBinding.PartyNameAndBinding = partyNameAndBinding;
            routeBinding.Key = await LoadTrackKeyAsync(track);
            routeBinding.ClaimMappings = track.ClaimMappings;
            routeBinding.SequenceLifetime = track.SequenceLifetime;
            routeBinding.MaxFailingLogins = track.MaxFailingLogins;
            routeBinding.FailingLoginCountLifetime = track.FailingLoginCountLifetime;
            routeBinding.FailingLoginObservationPeriod = track.FailingLoginObservationPeriod;
            routeBinding.PasswordLength = track.PasswordLength;
            routeBinding.CheckPasswordComplexity = track.CheckPasswordComplexity.Value;
            routeBinding.CheckPasswordRisk = track.CheckPasswordRisk.Value;
            routeBinding.AllowIframeOnDomains = track.AllowIframeOnDomains;

            if (!partyNameAndBinding.IsNullOrWhiteSpace())
            {
                var partyNameBindingMatch = partyNameBindingRegex.Match(partyNameAndBinding);
                if (!partyNameBindingMatch.Success)
                {
                    throw new ArgumentException($"Invalid party name and binding match. PartyNameAndBinding '{partyNameAndBinding}'");
                }

                if (partyNameBindingMatch.Groups["upparty"].Success)
                {
                    routeBinding.UpParty = await GetUpPartyAsync(tenantRepository, trackIdKey, partyNameBindingMatch.Groups["upparty"]);
                }
                else if (partyNameBindingMatch.Groups["downparty"].Success)
                {
                    routeBinding.DownParty = await GetDownPartyAsync(tenantRepository, trackIdKey, partyNameBindingMatch.Groups["downparty"]);

                    if (routeBinding.DownParty.AllowUpParties?.Count() >= 1)
                    {
                        if (partyNameBindingMatch.Groups["toupparty"].Success)
                        {
                            routeBinding.ToUpParties = await GetAllowedToUpPartyIds(scopedLogger, trackIdKey, partyNameBindingMatch.Groups["toupparty"], routeBinding.DownParty);
                        }
                        else
                        {
                            routeBinding.ToUpParties = routeBinding.DownParty.AllowUpParties.Take(1).ToList();
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

        private async Task<RouteTrackKey> LoadTrackKeyAsync(Track track)
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
                    // TODO load from key vault and save in radis
                    throw new NotImplementedException("load from key vault and save in radis...");

                case TrackKeyType.KeyVaultUpload:
                default:
                    throw new Exception($"Track key type not supported '{track.Key.Type}'.");
            }
        }

        private async Task<UpParty> GetUpPartyAsync(ITenantRepository tenantRepository, Track.IdKey trackIdKey, Group upPartyGroup)
        {
            var upPartyIdKey = new Party.IdKey
            {
                TenantName = trackIdKey.TenantName,
                TrackName = trackIdKey.TrackName,
                PartyName = upPartyGroup.Value,
            };

            try
            {
                return await tenantRepository.GetUpPartyByNameAsync(upPartyIdKey);
            }
            catch (Exception ex)
            {
                throw new RouteCreationException($"Invalid tenantName '{upPartyIdKey.TenantName}', trackName '{upPartyIdKey.TrackName}' and upPartyName '{upPartyIdKey.PartyName}'.", ex);
            }
        }

        private async Task<DownParty> GetDownPartyAsync(ITenantRepository tenantRepository, Track.IdKey trackIdKey, Group downPartyGroup)
        {
            var downPartyIdKey = new Party.IdKey
            {
                TenantName = trackIdKey.TenantName,
                TrackName = trackIdKey.TrackName,
                PartyName = downPartyGroup.Value,
            };

            try
            {
                return await tenantRepository.GetDownPartyByNameAsync(downPartyIdKey);
            }
            catch (Exception ex)
            {
                throw new RouteCreationException($"Invalid tenantName '{downPartyIdKey.TenantName}', trackName '{downPartyIdKey.TrackName}' and downPartyName '{downPartyIdKey.PartyName}'.", ex);
            }
        }

        private async Task<List<UpPartyLink>> GetAllowedToUpPartyIds(TelemetryScopedLogger scopedLogger, Track.IdKey trackIdKey, Group toUpPartyGroup, DownParty downParty)
        {
            var toUpParties = new List<UpPartyLink>();
            if (toUpPartyGroup.Captures.Count > 5)
            {
                throw new ArgumentException($"More then 5 to up party for down party '{downParty.Id}'.");
            }

            foreach (Capture upPartyCapture in toUpPartyGroup.Captures)
            {
                var toUpPartyIdKey = new Party.IdKey
                {
                    TenantName = trackIdKey.TenantName,
                    TrackName = trackIdKey.TrackName,
                    PartyName = upPartyCapture.Value,
                };
                await toUpPartyIdKey.ValidateObjectAsync();

                var allowUpParty = downParty.AllowUpParties.Where(ap => ap.Name == toUpPartyIdKey.PartyName).SingleOrDefault();
                if (allowUpParty != null)
                {
                    toUpParties.Add(allowUpParty);
                }
                else
                {
                    try
                    {
                        throw new ArgumentException($"Up Party name '{toUpPartyIdKey.PartyName}' not allowed for down party '{downParty.Id}',");
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.Warning(ex);
                    }
                }
            }

            if (toUpParties.Count() > 1)
            {
                throw new NotSupportedException("Currently only 0 and 1 to up party is supported.");
            }
            return toUpParties;
        }
    }
}
