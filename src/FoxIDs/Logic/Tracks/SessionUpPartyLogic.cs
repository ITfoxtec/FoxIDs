using FoxIDs.Infrastructure;
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
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SessionUpPartyLogic : SessionBaseLogic
    {
        private readonly TelemetryScopedLogger logger;
        private readonly SingleCookieRepository<SessionUpPartyCookie> sessionCookieRepository;

        public SessionUpPartyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, SingleCookieRepository<SessionUpPartyCookie> sessionCookieRepository, IHttpContextAccessor httpContextAccessor) : base(settings, httpContextAccessor)
        {
            this.logger = logger;
            this.sessionCookieRepository = sessionCookieRepository;
        }

        public async Task<List<Claim>> CreateOrUpdateSessionAsync<T>(T upParty, DownPartySessionLink newDownPartyLink, List<Claim> claims, string externalSessionId, string idToken = null) where T : UpParty
        {
            logger.ScopeTrace($"Create or update session up-party, Route '{RouteBinding.Route}'.");

            claims = FilterClaims(claims);

            Action<SessionUpPartyCookie> updateAction = (session) =>
            {
                claims.AddClaim(JwtClaimTypes.SessionId, NewSessionId());
                session.Claims = claims.ToClaimAndValues(); 

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
                    var userId = claims.FindFirstValue(c => c.Type == JwtClaimTypes.Subject);
                    if (!session.UserId.IsNullOrEmpty() && session.UserId != userId)
                    {
                        logger.ScopeTrace("Authenticated user and requested user do not match.");
                        // TODO invalid user login
                        throw new NotImplementedException("Authenticated user and requested user do not match.");
                    }

                    if (session.ExternalSessionId != externalSessionId)
                    {
                        try
                        {
                            throw new Exception("External session ID has changed, causing an session update including new session ID.");
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex);
                        }
                        updateAction(session);
                    }
                    else
                    {
                        claims.AddClaim(JwtClaimTypes.SessionId, session.SessionId);
                    }
                    session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await sessionCookieRepository.SaveAsync(session, null);
                    logger.ScopeTrace($"Session updated up-party, Session id '{session.SessionId}'.", GetSessionScopeProperties(session));
                }
                else
                {
                    await sessionCookieRepository.DeleteAsync();
                    logger.ScopeTrace($"Session deleted, Session id '{session.SessionId}'.");
                }
            }
            else
            {
                if (SessionEnabled(upParty))
                {
                    logger.ScopeTrace($"Create session up-party, External Session id '{externalSessionId}', Route '{RouteBinding.Route}'.");
                    session = new SessionUpPartyCookie();
                    updateAction(session);
                    session.LastUpdated = session.CreateTime;

                    await sessionCookieRepository.SaveAsync(session, null);
                    logger.ScopeTrace($"Session up-party created, User id '{session.UserId}', Session id '{session.SessionId}', External Session id '{externalSessionId}'.", GetSessionScopeProperties(session));
                }
            }

            return claims;
        }

        private List<Claim> FilterClaims(List<Claim> claims)
        {
            return claims.Where(c => c.Type == JwtClaimTypes.Subject || c.Type == JwtClaimTypes.Email || c.Type == JwtClaimTypes.Amr).ToList();
        }

        private string NewSessionId() => RandomGenerator.Generate(24);

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
                    logger.SetScopeProperty("userId", session.UserId);
                    logger.SetScopeProperty("email", session.Email);
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

        protected IDictionary<string, string> GetSessionScopeProperties(SessionUpPartyCookie session)
        {
            var scopeProperties = GetSessionScopeProperties(session as SessionBaseCookie);
            scopeProperties.Add("externalSessionId", session.ExternalSessionId);
            return scopeProperties;
        }
    }
}
