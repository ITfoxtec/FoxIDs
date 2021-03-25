using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Cookies;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SessionUpPartyLogic : SessionBaseLogic
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly SingleCookieRepository<SessionUpPartyCookie> sessionCookieRepository;

        public SessionUpPartyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, SingleCookieRepository<SessionUpPartyCookie> sessionCookieRepository, IHttpContextAccessor httpContextAccessor) : base(settings, httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.sessionCookieRepository = sessionCookieRepository;
        }

        public async Task CreateOrUpdateSessionAsync<T>(T upParty, DownPartyLink newDownPartyLink, IEnumerable<Claim> claims, string sessionId, string externalSessionId, string idToken = null) where T : UpParty
        {
            logger.ScopeTrace($"Create or update session up-party, Route '{RouteBinding.Route}'.");

            var userId = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Subject);

            Action<SessionUpPartyCookie> updateAction = (session) =>
            {
                var authMethods = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Amr).ToSpaceList();
                session.UserId = userId;
                session.Claims = claims.Where(c => c.Type != JwtClaimTypes.Subject && c.Type != JwtClaimTypes.Amr).ToClaimAndValues();
                session.AuthMethods = authMethods.ToList();
                session.SessionId = sessionId;
                session.ExternalSessionId = externalSessionId;
                session.IdToken = idToken;
                AddDownPartyLink(session, newDownPartyLink);
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
                session = new SessionUpPartyCookie();
                updateAction(session);
                session.LastUpdated = session.CreateTime;

                await sessionCookieRepository.SaveAsync(session, null);
                logger.ScopeTrace($"Session up-party created, Session id '{sessionId}', External Session id '{externalSessionId}'.", new Dictionary<string, string> { { "sessionId", session.SessionId }, { "externalSessionId", externalSessionId } });
            }
        }

        public async Task<SessionUpPartyCookie> GetSessionAsync<T>(T upParty) where T : UpParty
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

        public async Task<SessionUpPartyCookie> DeleteSessionAsync()
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
    }
}
