using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Cookies;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SessionLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly SingleCookieRepository<SessionCookie> sessionCookieRepository;

        public SessionLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ITenantRepository tenantRepository, SingleCookieRepository<SessionCookie> sessionCookieRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.sessionCookieRepository = sessionCookieRepository;
        }

        public async Task CreateSessionAsync(LoginUpParty loginUpParty, User user, long authTime, List<string> authMethods, string sessionId)
        {
            if(SessionEnabled(loginUpParty))
            {
                logger.ScopeTrace($"Create session for User '{user.Email}', User id '{user.UserId}', Session id '{sessionId}', Route '{RouteBinding.Route}'.");

                var session = new SessionCookie
                {
                    Email = user.Email,
                    UserId = user.UserId,
                    SessionId = sessionId
                };
                session.CreateTime = authTime;
                session.LastUpdated = authTime;
                session.AuthMethods = authMethods;
                await sessionCookieRepository.SaveAsync(session, GetPersistentCookieExpires(loginUpParty, session.CreateTime));
                logger.ScopeTrace($"Session created, Session id '{session.SessionId}'.", new Dictionary<string, string> { { "sessionId", session.SessionId } });
            }
        }

        public async Task<bool> UpdateSessionAsync(LoginUpParty loginUpParty, SessionCookie session)
        {
            logger.ScopeTrace($"Update session, Route '{RouteBinding.Route}'.");

            var sessionEnabled = SessionEnabled(loginUpParty);
            var sessionValid = SessionValid(loginUpParty, session);

            if (sessionEnabled && sessionValid)
            {
                session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await sessionCookieRepository.SaveAsync(session, GetPersistentCookieExpires(loginUpParty, session.CreateTime));
                logger.ScopeTrace($"Session updated, Session id '{session.SessionId}'.", new Dictionary<string, string> { { "sessionId", session.SessionId } });
                return true;
            }

            return false;
        }

        public async Task<(SessionCookie, User)> GetAndUpdateSessionCheckUserAsync(LoginUpParty loginUpParty)
        {
            logger.ScopeTrace($"Get and update session and check user, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync();
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(loginUpParty);
                var sessionValid = SessionValid(loginUpParty, session);

                logger.ScopeTrace($"User '{session.Email}' session exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', User id '{session.UserId}', Session id '{session.SessionId}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    logger.SetScopeProperty("sessionId", session.SessionId);
                    var id = await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = session.Email });
                    var user = await tenantRepository.GetAsync<User>(id, false);
                    if (user != null && user.UserId == session.UserId)
                    {
                        logger.ScopeTrace($"User '{user.Email}' found, User id '{user.UserId}'.");

                        session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        await sessionCookieRepository.SaveAsync(session, GetPersistentCookieExpires(loginUpParty, session.CreateTime));
                        logger.ScopeTrace($"Session updated, Session id '{session.SessionId}'.", new Dictionary<string, string> { { "sessionId", session.SessionId } });

                        return (session, user);
                    }
                }

                await sessionCookieRepository.DeleteAsync();
                logger.ScopeTrace($"Session deleted, Session id '{session.SessionId}'.");
            }
            else
            {
                logger.ScopeTrace("Session do not exists.");
            }

            return (null, null);
        }

        public async Task<SessionCookie> GetSessionAsync(LoginUpParty loginUpParty)
        {
            logger.ScopeTrace($"Get session, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync();
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(loginUpParty);
                var sessionValid = SessionValid(loginUpParty, session);

                logger.ScopeTrace($"User '{session.Email}' session exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', User id '{session.UserId}', Session id '{session.SessionId}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    logger.SetScopeProperty("sessionId", session.SessionId);
                    return session;
                }
            }
            else
            {
                logger.ScopeTrace("Session do not exists.");
            }

            return null;
        }

        public async Task<SessionCookie> DeleteSessionAsync(RouteBinding RouteBinding)
        {
            logger.ScopeTrace($"Delete session, Route '{RouteBinding.Route}'.");
            var session = await sessionCookieRepository.GetAsync();
            if (session != null)
            {
                await sessionCookieRepository.DeleteAsync();
                logger.ScopeTrace($"Session deleted, Session id '{session.SessionId}'.");
                return session;
            }
            else
            {
                logger.ScopeTrace("Session do not exists.");
                return null;
            }
        }

        public async Task TryDeleteSessionAsync()
        {
            try
            {
                var session = await sessionCookieRepository.GetAsync(tryGet: true);
                if (session != null)
                {
                    await sessionCookieRepository.DeleteAsync(tryDelete: true);
                }
            }
            catch
            { }
        }

        private bool SessionEnabled(LoginUpParty loginUpParty)
        {
            return loginUpParty.SessionLifetime > 0 || loginUpParty.PersistentSessionAbsoluteLifetime > 0 || loginUpParty.PersistentSessionLifetimeUnlimited.Value;
        }

        private DateTimeOffset? GetPersistentCookieExpires(LoginUpParty loginUpParty, long created)
        {
            if (loginUpParty.PersistentSessionLifetimeUnlimited.Value)
            {
                return DateTimeOffset.FromUnixTimeSeconds(created).AddYears(settings.PersistentSessionMaxUnlimitedLifetimeYears);
            }
            else if(loginUpParty.PersistentSessionAbsoluteLifetime > 0)
            {
                return DateTimeOffset.FromUnixTimeSeconds(created).AddSeconds(loginUpParty.PersistentSessionAbsoluteLifetime);
            }
            else
            {
                return null;
            }
        }

        private bool SessionValid(LoginUpParty loginUpParty, SessionCookie session)
        {
            var created = DateTimeOffset.FromUnixTimeSeconds(session.CreateTime);
            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(session.LastUpdated);
            var now = DateTimeOffset.UtcNow;

            if (loginUpParty.PersistentSessionLifetimeUnlimited.Value)
            {
                return true;
            }
            else if (created.AddSeconds(loginUpParty.PersistentSessionAbsoluteLifetime) >= now)
            {
                return true;
            }
            else if (lastUpdated.AddSeconds(loginUpParty.SessionLifetime) >= now && 
                (loginUpParty.SessionAbsoluteLifetime <= 0 || created.AddSeconds(loginUpParty.SessionAbsoluteLifetime) >= now))
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
