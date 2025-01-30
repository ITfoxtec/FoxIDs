using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public abstract class SessionBaseLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TrackCookieRepository<SessionTrackCookie> sessionTrackCookieRepository;

        public SessionBaseLogic(FoxIDsSettings settings, TrackCookieRepository<SessionTrackCookie> sessionTrackCookieRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.sessionTrackCookieRepository = sessionTrackCookieRepository;
        }

        protected bool SessionEnabled(IUpParty upParty)
        {
            return upParty.SessionLifetime > 0 || upParty.PersistentSessionAbsoluteLifetime > 0 || upParty.PersistentSessionLifetimeUnlimited;
        }

        public async Task AddOrUpdateSessionTrackAsync<T>(T upParty, DownPartySessionLink downPartyLink) where T : IUpParty
        {
            var session = await LoadSessionTrackAsync(upParty);
            if (!upParty.DisableSingleLogout && downPartyLink != null)
            {
                AddDownPartyLink(session, downPartyLink);
            }
            await sessionTrackCookieRepository.SaveAsync(session);
        }

        protected async Task AddOrUpdateSessionTrackWithClaimsAsync<T>(T upParty, IEnumerable<ClaimAndValues> claims) where T : IUpParty
        {
            var session = await LoadSessionTrackAsync(upParty);
            if (claims?.Count() > 0)
            {
                session.Claims = claims.Where(c => c.Claim == JwtClaimTypes.SessionId || c.Claim == JwtClaimTypes.Email || c.Claim == JwtClaimTypes.Subject || c.Claim == JwtClaimTypes.Name || c.Claim == Constants.JwtClaimTypes.Upn || c.Claim == Constants.JwtClaimTypes.SubFormat);
            }

            await sessionTrackCookieRepository.SaveAsync(session);
        }

        private async Task<SessionTrackCookie> LoadSessionTrackAsync<T>(T upParty) where T : IUpParty
        {
            var session = await sessionTrackCookieRepository.GetAsync();
            if (session == null)
            {
                session = new SessionTrackCookie();
                session.LastUpdated = session.CreateTime;
            }
            else
            {
                session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            AddUpPartyLink(session, upParty);
            return session;
        }

        protected async Task<string> GetSessionTrackSessionIdAsync()
        {
            var session = await sessionTrackCookieRepository.GetAsync();
            return session?.Claims?.Where(c => c.Claim == JwtClaimTypes.SessionId).Select(c => c.Values?.FirstOrDefault()).FirstOrDefault();
        }

        public async Task<SessionTrackCookie> GetAndDeleteSessionTrackCookieAsync()
        {
            var session = await sessionTrackCookieRepository.GetAsync();
            if (session != null)
            {
                await sessionTrackCookieRepository.DeleteAsync();
                return session;
            }
            return null;
        }

        private void AddUpPartyLink<T>(SessionTrackCookie session, T upParty) where T : IUpParty
        {
            if (upParty.DisableSingleLogout)
            {
                return;
            }

            var upPartySessionLink = new UpPartySessionLink { Id = upParty.Id, Type = upParty.Type };
            if (session.UpPartyLinks == null)
            {
                session.UpPartyLinks = new List<UpPartySessionLink> { upPartySessionLink };
            }
            else
            {
                if (!session.UpPartyLinks.Where(d => d.Id == upParty.Id).Any())
                {
                    session.UpPartyLinks.Add(upPartySessionLink);
                }
            }
        }

        protected void AddDownPartyLink(SessionTrackCookie session, DownPartySessionLink newDownPartyLink)
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
                { Constants.Logs.UserId, session.UserIdClaim },
                { Constants.Logs.Email, session.EmailClaim }
            };
            if (includeSessionId)
            {
                scopeProperties.Add(Constants.Logs.SessionId, session.SessionIdClaim);
            }
            return scopeProperties;
        }
    }
}
