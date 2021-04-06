using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SessionLoginUpPartyLogic : SessionBaseLogic
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly SingleCookieRepository<SessionLoginUpPartyCookie> sessionCookieRepository;

        public SessionLoginUpPartyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ITenantRepository tenantRepository, SingleCookieRepository<SessionLoginUpPartyCookie> sessionCookieRepository, IHttpContextAccessor httpContextAccessor) : base(settings, httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.sessionCookieRepository = sessionCookieRepository;
        }

        public async Task CreateSessionAsync(LoginUpParty loginUpParty, DownPartySessionLink newDownPartyLink, long authTime, List<Claim> claims)
        {
            if(SessionEnabled(loginUpParty))
            {
                logger.ScopeTrace($"Create session, Route '{RouteBinding.Route}'.");

                var session = new SessionLoginUpPartyCookie
                {
                    Claims = claims.ToClaimAndValues(),      
                };
                AddDownPartyLink(session, newDownPartyLink);
                session.CreateTime = authTime;
                session.LastUpdated = authTime;
                await sessionCookieRepository.SaveAsync(session, GetPersistentCookieExpires(loginUpParty, session.CreateTime));
                logger.ScopeTrace($"Session created, User id '{session.UserId}', Session id '{session.SessionId}'.", GetSessionScopeProperties(session));
            }
        }

        public async Task<bool> UpdateSessionAsync(LoginUpParty loginUpParty, DownPartySessionLink newDownPartyLink, SessionLoginUpPartyCookie session)
        {
            logger.ScopeTrace($"Update session, Route '{RouteBinding.Route}'.");

            var sessionEnabled = SessionEnabled(loginUpParty);
            var sessionValid = SessionValid(loginUpParty, session);

            if (sessionEnabled && sessionValid)
            {
                AddDownPartyLink(session, newDownPartyLink);
                session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await sessionCookieRepository.SaveAsync(session, GetPersistentCookieExpires(loginUpParty, session.CreateTime));
                logger.ScopeTrace($"Session updated, Session id '{session.SessionId}'.", GetSessionScopeProperties(session));
                return true;
            }

            await sessionCookieRepository.DeleteAsync();
            logger.ScopeTrace($"Session deleted, Session id '{session.SessionId}'.");
            return false;
        }

        public async Task<SessionLoginUpPartyCookie> GetAndUpdateSessionCheckUserAsync(LoginUpParty loginUpParty, DownPartySessionLink newDownPartyLink)
        {
            logger.ScopeTrace($"Get and update session and check user, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync();
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(loginUpParty);
                var sessionValid = SessionValid(loginUpParty, session);

                logger.ScopeTrace($"User id '{session.UserId}' session exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Session id '{session.SessionId}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    var id = await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = session.Email });
                    var user = await tenantRepository.GetAsync<User>(id, false);
                    if (user != null && user.UserId == session.UserId)
                    {
                        AddDownPartyLink(session, newDownPartyLink);
                        session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        await sessionCookieRepository.SaveAsync(session, GetPersistentCookieExpires(loginUpParty, session.CreateTime));
                        logger.ScopeTrace($"Session updated, Session id '{session.SessionId}'.", GetSessionScopeProperties(session));

                        return session;
                    }
                }

                await sessionCookieRepository.DeleteAsync();
                logger.ScopeTrace($"Session deleted, Session id '{session.SessionId}'.");
            }
            else
            {
                logger.ScopeTrace("Session do not exists.");
            }

            return null;
        }

        public async Task<SessionLoginUpPartyCookie> GetSessionAsync(LoginUpParty loginUpParty)
        {
            logger.ScopeTrace($"Get session, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync();
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(loginUpParty);
                var sessionValid = SessionValid(loginUpParty, session);

                logger.ScopeTrace($"User id '{session.UserId}' session exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Session id '{session.SessionId}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    logger.SetScopeProperty("sessionId", session.SessionId);
                    logger.SetScopeProperty("userId", session.UserId);
                    logger.SetScopeProperty("email", session.Email);
                    return session;
                }

                await sessionCookieRepository.DeleteAsync();
                logger.ScopeTrace($"Session deleted, Session id '{session.SessionId}'.");
            }
            else
            {
                logger.ScopeTrace("Session do not exists.");
            }

            return null;
        }

        public async Task<SessionLoginUpPartyCookie> DeleteSessionAsync()
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
    }
}
