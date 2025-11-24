using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
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
        private readonly ActiveSessionLogic activeSessionLogic;

        public SessionBaseLogic(FoxIDsSettings settings, TrackCookieRepository<SessionTrackCookie> sessionTrackCookieRepository, ActiveSessionLogic activeSessionLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.sessionTrackCookieRepository = sessionTrackCookieRepository;
            this.activeSessionLogic = activeSessionLogic;
        }

        public async Task<string> GetSessionIdAsync<T>(T upParty) where T : IUpParty
        {
            var session = await sessionTrackCookieRepository.GetAsync();
            if (session != null && session.Groups?.Count() > 0)
            {
                var sessionGroup = session.Groups.Where(g => (g.SequenceId != null && g.SequenceId == Sequence?.Id) || g.UpPartyLinks?.Any(u => u.Id == upParty.Id) == true).FirstOrDefault();
                if (sessionGroup != null)
                {
                    var sessionId = sessionGroup.Claims?.Where(c => c.Claim == JwtClaimTypes.SessionId).Select(c => c.Values?.FirstOrDefault()).FirstOrDefault();
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        return sessionId;
                    }
                }
            }

            return CreatSessionId(upParty);
        }

        public async Task<(List<UpPartyLink>, bool isSession)> GetSessionOrRouteBindingUpParty(List<UpPartyLink> upPartyLinks)
        {
            var session = await sessionTrackCookieRepository.GetAsync();
            if (session != null && session.Groups?.Count() > 0)
            {
                var sessionGroup = session.Groups.Where(g => g.SessionUpParty != null && g.UpPartyLinks?.Any(u => upPartyLinks.Any(ru => ru.Name == u.Id.PartyIdToName())) == true).FirstOrDefault();
                if (sessionGroup != null && sessionGroup.SessionUpParty != null)
                {
                    return ([new UpPartyLink { Name = sessionGroup.SessionUpParty.Id.PartyIdToName(), Type = sessionGroup.SessionUpParty.Type }], true);
                }
            }
            return (upPartyLinks, false);
        }

        private string CreatSessionId<T>(T upParty) where T : IUpParty
        {
            var baseSessionId = RandomGenerator.Generate(24);
            if (upParty.SessionLifetime > Constants.Models.Session.sessionShortLongThreshold || upParty.PersistentSessionAbsoluteLifetime > Constants.Models.Session.sessionShortLongThreshold || upParty.PersistentSessionLifetimeUnlimited)
            {
                return $"{baseSessionId}{Constants.Models.Session.LongSessionPostKey}";
            }
            else
            {
                return $"{baseSessionId}{Constants.Models.Session.ShortSessionPostKey}";
            }
        }

        protected bool SessionEnabled<T>(T upParty) where T : IUpParty
        {
            return upParty.SessionLifetime > 0 || upParty.PersistentSessionAbsoluteLifetime > 0 || upParty.PersistentSessionLifetimeUnlimited;
        }

        public async Task AddOrUpdateSessionTrackAsync<T>(T upParty, DownPartySessionLink downPartyLink, bool updateDbActiveSession = false) where T : IUpParty
        {
            (var session, var sessionGroups) = await LoadSessionTrackAsync(upParty, downPartyLink);
            foreach (var sessionGroup in sessionGroups)
            {
                AddUpPartyLink(sessionGroup, upParty);
                if (!upParty.DisableSingleLogout && downPartyLink != null)
                {
                    AddDownPartyLink(sessionGroup, downPartyLink);
                }
            }
            await sessionTrackCookieRepository.SaveAsync(session);

            if (updateDbActiveSession)
            {
                await activeSessionLogic.SaveSessionAsync(sessionGroups, session.CreateTime, session.LastUpdated);
            }
        }

        protected async Task AddOrUpdateSessionTrackWithClaimsAsync<T>(T upParty, IEnumerable<ClaimAndValues> claims, bool updateDbActiveSession = false) where T : IUpParty
        {
            (var session, var sessionGroups) = await LoadSessionTrackAsync(upParty, null);
            foreach (var sessionGroup in sessionGroups)
            {
                AddUpPartyLink(sessionGroup, upParty);
                if (sessionGroup.SessionUpParty == null && claims?.Count() > 0)
                {
                    sessionGroup.SessionUpParty = new UpPartySessionLink { Id = upParty.Id, Type = upParty.Type };
                    sessionGroup.Claims = claims.Where(c => c.Claim == JwtClaimTypes.SessionId || c.Claim == JwtClaimTypes.Email || c.Claim == JwtClaimTypes.PreferredUsername || c.Claim == JwtClaimTypes.PhoneNumber || c.Claim == JwtClaimTypes.Subject || c.Claim == Constants.JwtClaimTypes.SubFormat);
                }
            }
            await sessionTrackCookieRepository.SaveAsync(session);

            if (updateDbActiveSession && claims?.Any() == true)
            {
                await activeSessionLogic.SaveSessionAsync(sessionGroups, session.CreateTime, session.LastUpdated);
            }
        }

        private async Task<(SessionTrackCookie, IEnumerable<SessionTrackCookieGroup>)> LoadSessionTrackAsync<T>(T upParty, DownPartySessionLink downPartyLink) where T : IUpParty
        {
            var session = await sessionTrackCookieRepository.GetAsync();
            if (session == null)
            {
                session = new SessionTrackCookie();
                session.LastUpdated = session.CreateTime;
                return (session, AddNewSessionTrackCookieSequenceGroups(session));
            }
            else
            {
                session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var sessionGroups = session.Groups?.Where(g => (downPartyLink != null && g.DownPartyLinks?.Any(d => d.Id == downPartyLink.Id) == true) || (g.SequenceId  != null && g.SequenceId == Sequence?.Id) || g.UpPartyLinks?.Any(u => u.Id == upParty.Id) == true);
                if (!(sessionGroups?.Count() > 0))
                {
                    sessionGroups = AddNewSessionTrackCookieSequenceGroups(session);
                }
                return (session, sessionGroups);
            }
        }

        protected async Task<bool> ActiveSessionExistsAsync(IEnumerable<ClaimAndValues> claims)
        {
            try
            {
                await activeSessionLogic.ValidateSessionAsync(claims.ToClaimList());
                return true;
            }
            catch (SessionException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private IEnumerable<SessionTrackCookieGroup> AddNewSessionTrackCookieSequenceGroups(SessionTrackCookie session)
        {
            if (session.Groups == null)
            {
                session.Groups = new List<SessionTrackCookieGroup>();
            }
            var sessionGroup = new SessionTrackCookieGroup { SequenceId = Sequence?.Id };
            if (session.Groups.Count < Constants.Models.Session.GroupsMax)
            {
                session.Groups.Add(sessionGroup);
            }
            return [sessionGroup];
        }

        public async Task<SessionTrackCookieGroup> GetAndDeleteSessionTrackCookieGroupAsync<T>(T upParty) where T : IUpParty
        {
            var session = await sessionTrackCookieRepository.GetAsync();
            if (session != null && session.Groups?.Count() > 0)
            {
                var removedGroups = session.Groups.Where(g => g.UpPartyLinks?.Any(u => u.Id == upParty.Id) == true).ToList();
                if (removedGroups.Count > 0)
                {
                    session.Groups.RemoveAll(g => g.UpPartyLinks?.Any(u => u.Id == upParty.Id) == true);
                    await sessionTrackCookieRepository.SaveAsync(session);
                    await DeleteActiveSessionsAsync(removedGroups);
                    return removedGroups.First();
                }
            }
            return null;
        }

        public async Task DeleteSessionTrackCookieGroupAsync<T>(T upParty) where T : IUpParty
        {
            var session = await sessionTrackCookieRepository.GetAsync();
            if (session != null && session.Groups?.Count() > 0)
            {
                var removedGroups = session.Groups.Where(g => g.UpPartyLinks?.Any(u => u.Id == upParty.Id) == true).ToList();
                if (removedGroups.Count > 0)
                {
                    session.Groups.RemoveAll(g => g.UpPartyLinks?.Any(u => u.Id == upParty.Id) == true);
                    await sessionTrackCookieRepository.SaveAsync(session);
                    await DeleteActiveSessionsAsync(removedGroups);
                }
            }
        }

        private async Task DeleteActiveSessionsAsync(IEnumerable<SessionTrackCookieGroup> sessionGroups)
        {
            var sessionIds = sessionGroups?
                .Where(g => g?.Claims != null)
                .SelectMany(g => g.Claims.Where(c => c.Claim == JwtClaimTypes.SessionId && c.Values != null))
                .SelectMany(c => c.Values)
                .Where(id => !id.IsNullOrWhiteSpace())
                .Distinct(StringComparer.Ordinal);

            if (sessionIds?.Any() == true)
            {
                foreach (var sessionId in sessionIds)
                {
                    await activeSessionLogic.DeleteSessionAsync(sessionId);
                }
            }
        }

        private void AddUpPartyLink<T>(SessionTrackCookieGroup sessionGroup, T upParty) where T : IUpParty
        {
            if (upParty.DisableSingleLogout)
            {
                return;
            }

            var upPartySessionLink = new UpPartySessionLink { Id = upParty.Id, Type = upParty.Type };
            if (sessionGroup.UpPartyLinks == null)
            {
                sessionGroup.UpPartyLinks = new List<UpPartySessionLink> { upPartySessionLink };
            }
            else
            {
                if (!sessionGroup.UpPartyLinks.Where(d => d.Id == upParty.Id).Any())
                {
                    sessionGroup.UpPartyLinks.Add(upPartySessionLink);
                }
            }
        }

        protected void AddDownPartyLink(SessionTrackCookieGroup sessionGroup, DownPartySessionLink downPartyLink)
        {
            if (downPartyLink == null)
            {
                return;
            }

            if (sessionGroup.DownPartyLinks == null)
            {
                sessionGroup.DownPartyLinks = new List<DownPartySessionLink> { downPartyLink };
            }
            else
            {
                if (!sessionGroup.DownPartyLinks.Where(d => d.Id == downPartyLink.Id).Any())
                {
                    sessionGroup.DownPartyLinks.Add(downPartyLink);
                }
            }
        }

        protected DateTimeOffset? GetPersistentCookieExpires<T>(T upParty, long created) where T : IUpParty
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

        protected bool SessionValid<T>(CookieMessage session, T upParty) where T : IUpParty
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
