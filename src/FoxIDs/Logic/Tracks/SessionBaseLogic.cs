using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Session;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Logic
{
    public abstract class SessionBaseLogic : LogicSequenceBase
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

        protected void AddDownPartyLink(SessionBaseCookie session, DownPartySessionLink newDownPartyLink)
        {
            if (newDownPartyLink == null || !newDownPartyLink.SupportSingleLogout)
            {
                return;
            }

            if (session.DownPartyLinks == null)
            {
                session.DownPartyLinks = new List<DownPartySessionLink> { newDownPartyLink };
            }
            else
            {
                if (!session.DownPartyLinks.Where(d => d.Id == newDownPartyLink.Id).Any())
                {
                    session.DownPartyLinks.Add(newDownPartyLink);
                }
            }
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

        protected bool SessionValid(CookieMessage session, UpParty upParty)
        {
            return SessionValid(session, upParty.SessionLifetime, upParty.SessionAbsoluteLifetime, upParty.PersistentSessionAbsoluteLifetime, upParty.PersistentSessionLifetimeUnlimited);
        }

        protected bool SessionValid(CookieMessage session, int sessionLifetime, int sessionAbsoluteLifetime, int persistentSessionAbsoluteLifetime, bool persistentSessionLifetimeUnlimited)
        {
            var created = DateTimeOffset.FromUnixTimeSeconds(session.CreateTime);
            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(session.LastUpdated);
            var now = DateTimeOffset.UtcNow;

            if (persistentSessionLifetimeUnlimited)
            {
                return true;
            }
            else if (created.AddSeconds(persistentSessionAbsoluteLifetime) >= now)
            {
                return true;
            }
            else if (lastUpdated.AddSeconds(sessionLifetime) >= now &&
                (sessionAbsoluteLifetime <= 0 || created.AddSeconds(sessionAbsoluteLifetime) >= now))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected IDictionary<string, string> GetSessionScopeProperties(SessionBaseCookie session)
        {
            return new Dictionary<string, string>
            {
                { "sessionId", session.SessionId },
                { "userId", session.UserId },
                { "email", session.Email }
            };
        }
    }
}
