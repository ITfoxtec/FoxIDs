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

        protected bool SessionEnabled(IUpParty upParty)
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

        protected DateTimeOffset? GetPersistentCookieExpires(IUpParty upParty, long created)
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

        protected bool SessionValid(CookieMessage session, IUpParty upParty)
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

        protected IDictionary<string, string> GetSessionScopeProperties(SessionBaseCookie session, bool includeSessionId = true)
        {
            var scopeProperties = new Dictionary<string, string>
            {
                { Constants.Logs.UserId, session.UserId },
                { Constants.Logs.Email, session.Email }
            };
            if (includeSessionId)
            {
                scopeProperties.Add(Constants.Logs.SessionId, session.SessionId);
            }
            return scopeProperties;
        }
    }
}
