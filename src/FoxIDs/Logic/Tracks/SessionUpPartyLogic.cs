using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Cookies;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SessionUpPartyLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly SingleCookieRepository<SessionUpParty> sessionCookieRepository;

        public SessionUpPartyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, SingleCookieRepository<SessionUpParty> sessionCookieRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.sessionCookieRepository = sessionCookieRepository;
        }

        public async Task CreateOrUpdateSessionAsync<T>(T upParty, List<Claim> claims, string sessionId, string externalSessionId, string idToken = null) where T : UpParty, ISessionUpParty
        {
            logger.ScopeTrace($"Create or update session up-party, Route '{RouteBinding.Route}'.");

            var userId = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Subject);
            var authMethods = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Amr).ToSpaceList();

            Action<SessionUpParty> updateAction = (session) =>
            {
                session.UserId = userId;
                session.Claims = claims.ToClaimAndValues();
                session.AuthMethods = authMethods;
                session.SessionId = sessionId;
                session.ExternalSessionId = externalSessionId;
                session.IdToken = idToken;
            };

            var session = await sessionCookieRepository.GetAsync();
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(upParty);
                var sessionValid = SessionValid(upParty, session);

                logger.ScopeTrace($"User id '{session.UserId}' session up-party exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Session id '{session.SessionId}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    if (!session.UserId.IsNullOrEmpty() && session.UserId != userId)
                    {
                        logger.ScopeTrace("Authenticated user and requested user do not match.");
                        // TODO invalid user login
                        throw new NotImplementedException("Authenticated user and requested user do not match.");
                    }
                    updateAction(session);
                    session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await sessionCookieRepository.SaveAsync(session, null);
                    logger.ScopeTrace($"Session updated up-party, Session id '{session.SessionId}', External Session id '{session.ExternalSessionId}'.", new Dictionary<string, string> { { "sessionId", session.SessionId }, { "externalSessionId", session.ExternalSessionId } });
                }
                else
                {
                    await sessionCookieRepository.DeleteAsync();
                    logger.ScopeTrace($"Session deleted, Session id '{session.SessionId}'.");
                }
            }
            else
            {
                if (!SessionEnabled(upParty))
                {
                    return;
                }

                logger.ScopeTrace($"Create session up-party for User id '{userId}', Session id '{sessionId}', External Session id '{externalSessionId}', Route '{RouteBinding.Route}'.");
                session = new SessionUpParty();
                updateAction(session);
                session.LastUpdated = session.CreateTime;

                await sessionCookieRepository.SaveAsync(session, null);
                logger.ScopeTrace($"Session up-party created, Session id '{sessionId}', External Session id '{externalSessionId}'.", new Dictionary<string, string> { { "sessionId", session.SessionId }, { "externalSessionId", externalSessionId } });
            }
        }

        public async Task<SessionUpParty> GetSessionAsync<T>(T upParty) where T : UpParty, ISessionUpParty
        {
            logger.ScopeTrace($"Get session up-party, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync();
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(upParty);
                var sessionValid = SessionValid(upParty, session);

                logger.ScopeTrace($"User id '{session.UserId}' session up-party exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Session id '{session.SessionId}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    logger.SetScopeProperty("sessionId", session.SessionId);
                    logger.SetScopeProperty("externalSessionId", session.ExternalSessionId);
                    return session;
                }

                await sessionCookieRepository.DeleteAsync();
                logger.ScopeTrace($"Session deleted, Session id '{session.SessionId}'.");
            }
            else
            {
                logger.ScopeTrace("Session up-party do not exists.");
            }

            return null;
        }

        public async Task<SessionUpParty> DeleteSessionAsync<T>(T upParty) where T : UpParty, ISessionUpParty
        {
            logger.ScopeTrace($"Delete session up-party, Route '{RouteBinding.Route}'.");
            var session = await sessionCookieRepository.GetAsync();
            if (session != null)
            {
                await sessionCookieRepository.DeleteAsync();
                logger.ScopeTrace($"Session deleted up-party, Session id '{session.SessionId}'.");
                return session;
            }
            else
            {
                logger.ScopeTrace("Session up-party do not exists.");
                return null;
            }
        }

        private bool SessionEnabled(ISessionUpParty sessionUpParty)
        {
            return sessionUpParty.SessionLifetime > 0;
        }

        private bool SessionValid(ISessionUpParty sessionUpParty, SessionUpParty session)
        {
            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(session.LastUpdated);
            var now = DateTimeOffset.UtcNow;

            if (lastUpdated.AddSeconds(sessionUpParty.SessionLifetime.Value) >= now)
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
