using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using ITfoxtec.Identity;
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

        public async Task SaveHrdSelectionAsync(string loginUpPartyName, string selectedUpPartyName, string selectedUpPartyProfileName, PartyTypes selectedType)
        {
            logger.ScopeTrace(() => $"Save HRD selection, Route '{RouteBinding.Route}'.");

            var hrdSelectionCookie = await hrdSelectionCookieRepository.GetAsync();
            if (hrdSelectionCookie == null)
            {
                hrdSelectionCookie = new HrdTrackCookie
                {
                    UpParties = [new HrdUpPartyCookieData { LoginUpPartyName = loginUpPartyName, SelectedUpPartyName = selectedUpPartyName, SelectedUpPartyProfileName = selectedUpPartyProfileName, SelectedUpPartyType = selectedType }]
                };
            }
            else
            {
                var newUpParties = new List<HrdUpPartyCookieData> { new HrdUpPartyCookieData { LoginUpPartyName = loginUpPartyName, SelectedUpPartyName = selectedUpPartyName, SelectedUpPartyProfileName = selectedUpPartyProfileName, SelectedUpPartyType = selectedType } };
                var existingUpPartis = hrdSelectionCookie.UpParties.Where(up => loginUpPartyName.IsNullOrEmpty() || up.LoginUpPartyName != loginUpPartyName);
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
            logger.ScopeTrace(() => $"HRD selection saved, login authentication method name '{loginUpPartyName}', selected authentication method name '{selectedUpPartyName}' and profile '{selectedUpPartyProfileName}' (optional) and type '{selectedType}'.");
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

        public (string loginName, IEnumerable<UpPartyLink> toUpParties) GetLoginUpPartyNameAndToUpParties()
        {
            var toUpParties = RouteBinding.ToUpParties;
            var login = toUpParties.Where(up => up.Type == PartyTypes.Login).FirstOrDefault();
            var loginName = login != null ? login.Name : Constants.DefaultLogin.Name;

            return (loginName, toUpParties);
        }

        public async Task<UpPartyLink> GetUpPartyAndDeleteHrdSelectionAsync()
        {
            logger.ScopeTrace(() => $"Get authentication method and delete in HRD selection, Route '{RouteBinding.Route}'.");

            var toUpParties = RouteBinding.ToUpParties;
            var hrdSelectionCookie = await hrdSelectionCookieRepository.GetAsync();
            if (hrdSelectionCookie != null && hrdSelectionCookie.UpParties.Count() > 0)
            {
                foreach (var toUpParty in toUpParties)
                {
                    var hrdUpParty = hrdSelectionCookie.UpParties.Where(up => (up.SelectedUpPartyName == toUpParty.Name && (up.SelectedUpPartyProfileName.IsNullOrEmpty() || up.SelectedUpPartyProfileName == toUpParty.ProfileName)) || 
                        (!up.LoginUpPartyName.IsNullOrEmpty() && up.LoginUpPartyName == toUpParty.Name && toUpParties.Where(tup => tup.Name == up.SelectedUpPartyName && (up.SelectedUpPartyProfileName.IsNullOrEmpty() || tup.ProfileName == up.SelectedUpPartyProfileName)).Any())).FirstOrDefault();
                    if (hrdUpParty != null)
                    {
                        await DeleteHrdSelectionBySelectedUpPartyAsync(hrdUpParty.SelectedUpPartyName, hrdUpParty.SelectedUpPartyProfileName, hrdSelectionCookie);
                        return new UpPartyLink { Name = hrdUpParty.SelectedUpPartyName, ProfileName = hrdUpParty.SelectedUpPartyProfileName, Type = hrdUpParty.SelectedUpPartyType };
                    }
                }
            }

            return toUpParties.First();
        }

        public async Task DeleteHrdSelectionBySelectedUpPartyAsync(string selectedUpPartyName, string selectedUpPartyProfileName = null, HrdTrackCookie hrdSelectionCookie = null)
        {
            logger.ScopeTrace(() => $"Delete in HRD selection by selected authentication method, Route '{RouteBinding.Route}'.");

            hrdSelectionCookie = hrdSelectionCookie ?? await hrdSelectionCookieRepository.GetAsync();
            if (hrdSelectionCookie != null && hrdSelectionCookie.UpParties.Count() > 0)
            {
                var otherUpPartis = hrdSelectionCookie.UpParties.Where(up => up.SelectedUpPartyName != selectedUpPartyName && (selectedUpPartyProfileName.IsNullOrEmpty() || up.SelectedUpPartyProfileName != selectedUpPartyProfileName));
                if (otherUpPartis.Count() <= 0)
                {
                    await hrdSelectionCookieRepository.DeleteAsync();
                    logger.ScopeTrace(() => $"HRD selection deleted, selected authentication method name '{selectedUpPartyName}' deleted.");
                }
                else if (hrdSelectionCookie.UpParties.Count() != otherUpPartis.Count())
                {
                    hrdSelectionCookie.UpParties = otherUpPartis;
                    await hrdSelectionCookieRepository.SaveAsync(hrdSelectionCookie);
                    logger.ScopeTrace(() => $"HRD selection updated, selected authentication method name '{selectedUpPartyName}' deleted.");
                }
            }
        }
    }
}
