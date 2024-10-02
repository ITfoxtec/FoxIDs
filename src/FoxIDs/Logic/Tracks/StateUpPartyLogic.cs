using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class StateUpPartyLogic : SessionBaseLogic
    {
        private readonly TelemetryScopedLogger logger;
        private readonly UpPartyCookieRepository<StateUpPartyCookie> stateCookieRepository;

        public StateUpPartyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, UpPartyCookieRepository<StateUpPartyCookie> stateCookieRepository, IHttpContextAccessor httpContextAccessor) : base(settings, httpContextAccessor)
        {
            this.logger = logger;
            this.stateCookieRepository = stateCookieRepository;
        }

        public async Task CreateOrUpdateStateCookieAsync<T>(T upParty, string state) where T : UpParty
        {
            logger.ScopeTrace(() => $"Create or update authentication method state cookie, Route '{RouteBinding.Route}'.");

            Action<StateUpPartyCookie> updateAction = (stateCookie) =>
            {
                stateCookie.State = state; 
            };

            var stateCookie = await stateCookieRepository.GetAsync(upParty);
            if (stateCookie != null)
            {
                updateAction(stateCookie);
                stateCookie.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            else
            {
                stateCookie = new StateUpPartyCookie();
                updateAction(stateCookie);
                stateCookie.LastUpdated = stateCookie.CreateTime;
            }

            await stateCookieRepository.SaveAsync(upParty, stateCookie, null);
        }

        public async Task<string> GetAndDeleteStateAsync<T>(T upParty) where T : UpParty
        {
            logger.ScopeTrace(() => $"Get and delete authentication method state cookie, Route '{RouteBinding.Route}'.");

            var stateCookie = await stateCookieRepository.GetAsync(upParty);
            if(stateCookie != null)
            {
                await stateCookieRepository.DeleteAsync(upParty);
            }
            return stateCookie.State;            
        }
    }
}
