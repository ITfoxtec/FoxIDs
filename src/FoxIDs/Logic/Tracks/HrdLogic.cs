using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Tracks
{
    public class HrdLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly TrackCookieRepository<HrdTrackCookie> hrdSelectionCookieRepository;

        public HrdLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, TrackCookieRepository<HrdTrackCookie> hrdSelectionCookieRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.hrdSelectionCookieRepository = hrdSelectionCookieRepository;
        }

        public async Task SaveHrdSelectionAsync(string upPartyName, PartyTypes type)
        {
            logger.ScopeTrace(() => $"Save HRD selection, Route '{RouteBinding.Route}'.");

            var hrdSelectionCookie = await hrdSelectionCookieRepository.GetAsync();
            if (hrdSelectionCookie == null)
            {
                hrdSelectionCookie = new HrdTrackCookie
                {
                    UpParties = new [] { new HrdUpPartyCookieData { Name = upPartyName, Type = type } }
                };
            }
            else
            {
                var newUpParties = new List<HrdUpPartyCookieData> { new HrdUpPartyCookieData { Name = upPartyName, Type = type } };
                var existingUpPartis = hrdSelectionCookie.UpParties.Where(up => up.Name != upPartyName);
                if (existingUpPartis.Any())
                {
                    newUpParties.AddRange(existingUpPartis);
                }
                hrdSelectionCookie.UpParties = newUpParties;

                if (hrdSelectionCookie.UpParties.Count() > settings.HrdUpPartiesMaxCount)
                {
                    hrdSelectionCookie.UpParties = hrdSelectionCookie.UpParties.Take(settings.HrdUpPartiesMaxCount);
                }
            }

            await hrdSelectionCookieRepository.SaveAsync(hrdSelectionCookie);
            logger.ScopeTrace(() => $"HRD selection saved, up-party name '{upPartyName}', type '{type}'.");
        }

        public async Task<IEnumerable<HrdUpPartyCookieData>> GetHrdSelectionAsync()
        {
            var hrdSelectionCookie = await hrdSelectionCookieRepository.GetAsync();
            if (hrdSelectionCookie != null)
            {
                return hrdSelectionCookie.UpParties;
            }

            return null;
        }

    
    }
}
