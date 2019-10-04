using ITfoxtec.Identity;
using FoxIDs.Models;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FoxIDs.Infrastructure.Hosting
{
    public abstract class SiteRouter : IRouter
    {
        private static Regex partyNameBindingRegex = new Regex(@"^(?:(?:(?<downparty>[\w-_]+)(?:\((?:(?:(?<toupparty>[\w-_]+)(?:,(?<toupparty>[\w-_]+))*)|(?<toupparty>\*))\))?)|(?:\((?<upparty>[\w-_]+)\)))$", RegexOptions.Compiled);
        private readonly IRouter defaultRouter;

        public SiteRouter(IRouter defaultRouter)
        {
            this.defaultRouter = defaultRouter;
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return defaultRouter.GetVirtualPath(context);
        }

        public async Task RouteAsync(RouteContext context)
        {
            try
            {
                await HandleRouteAsync(context);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failing url '{context.HttpContext?.Request?.Path.Value}'", ex);
            }

            await defaultRouter.RouteAsync(context);
        }
        
        protected abstract Task HandleRouteAsync(RouteContext context);
      
        protected async Task<RouteBinding> GetRouteDataAsync(TelemetryScopedLogger scopedLogger, HttpContext httpContext, Track.IdKey trackIdKey, string partyNameAndBinding = null)
        {
            var tenantRepository = httpContext.RequestServices.GetService<ITenantRepository>();

            var track = await GetTrackAsync(tenantRepository, trackIdKey);
            var routeBinding = new RouteBinding
            {
                RouteUrl = $"{trackIdKey.TenantName}{$"/{trackIdKey.TrackName}{(!partyNameAndBinding.IsNullOrWhiteSpace() ? $"/{partyNameAndBinding}" : string.Empty)}"}",
                TenantName = trackIdKey.TenantName,
                TrackName = trackIdKey.TrackName,
                PartyNameAndBinding = partyNameAndBinding,
                PrimaryKey = track.PrimaryKey,
                SecondaryKey = track.SecondaryKey,
                ClaimMappings = track.ClaimMappings,
                Resources = track.Resources,
                SequenceLifetime = track.SequenceLifetime,
                PasswordLength = track.PasswordLength,
                CheckPasswordComplexity = track.CheckPasswordComplexity.Value,
                CheckPasswordRisk = track.CheckPasswordRisk.Value
            };

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

        private async Task<Track> GetTrackAsync(ITenantRepository tenantRepository, Track.IdKey idKey)
        {
            try
            {
                return await tenantRepository.GetTrackByNameAsync(idKey);
            }
            catch (Exception ex)
            {
                throw new RouteCreationException($"Invalid tenantName '{idKey.TenantName}' and trackName '{idKey.TrackName}'.", ex);
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
