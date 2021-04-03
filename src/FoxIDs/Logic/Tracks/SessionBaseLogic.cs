using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Cookies;
using Microsoft.AspNetCore.Http;
using System;

namespace FoxIDs.Logic
{
    public abstract class SessionBaseLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;

        public SessionBaseLogic(FoxIDsSettings settings, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
        }

        protected bool SessionEnabled(UpParty upParty)
        {
            return upParty.SessionLifetime > 0 || upParty.PersistentSessionAbsoluteLifetime > 0 || upParty.PersistentSessionLifetimeUnlimited;
        }

        protected DateTimeOffset? GetPersistentCookieExpires(UpParty upParty, long created)
        {
            if (upParty.PersistentSessionLifetimeUnlimited)
            {
                return DateTimeOffset.FromUnixTimeSeconds(created).AddYears(settings.PersistentSessionMaxUnlimitedLifetimeYears);
            }
            else if(upParty.PersistentSessionAbsoluteLifetime > 0)
            {
                return DateTimeOffset.FromUnixTimeSeconds(created).AddSeconds(upParty.PersistentSessionAbsoluteLifetime);
            }
            else
            {
                return null;
            }
        }

        protected bool SessionValid(UpParty upParty, SessionBaseCookie session)
        {
            var created = DateTimeOffset.FromUnixTimeSeconds(session.CreateTime);
            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(session.LastUpdated);
            var now = DateTimeOffset.UtcNow;

            if (upParty.PersistentSessionLifetimeUnlimited)
            {
                return true;
            }
            else if (created.AddSeconds(upParty.PersistentSessionAbsoluteLifetime) >= now)
            {
                return true;
            }
            else if (lastUpdated.AddSeconds(upParty.SessionLifetime) >= now && 
                (upParty.SessionAbsoluteLifetime <= 0 || created.AddSeconds(upParty.SessionAbsoluteLifetime) >= now))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
