using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider serviceProvider;
        private readonly UpPartyCookieRepository<SessionUpPartyCookie> sessionCookieRepository;

        public SessionUpPartyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, UpPartyCookieRepository<SessionUpPartyCookie> sessionCookieRepository, TrackCookieRepository<SessionTrackCookie> sessionTrackCookieRepository, ActiveSessionLogic activeSessionLogic, IHttpContextAccessor httpContextAccessor) : base(settings, sessionTrackCookieRepository, activeSessionLogic, httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sessionCookieRepository = sessionCookieRepository;
        }

        public async Task CreateOrUpdateMarkerSessionAsync<T>(T upParty, DownPartySessionLink downPartyLink, string externalSessionId, string idToken = null, List<Claim> samlClaims = null) where T : UpParty
        {
            await AddOrUpdateSessionTrackAsync(upParty, downPartyLink);

            logger.ScopeTrace(() => $"Create marker session for authentication method, Route '{RouteBinding.Route}'.");
            var session = new SessionUpPartyCookie();
            session.IsMarkerSession = true;
            session.IdToken = idToken;
            session.Claims = await GetJwtClaimsAsync(samlClaims);
            session.ExternalSessionId = externalSessionId;
            session.LastUpdated = session.CreateTime;
            await sessionCookieRepository.SaveAsync(upParty, session, null);
        }

        private async Task<IEnumerable<ClaimAndValues>> GetJwtClaimsAsync(List<Claim> samlClaims)
        {
            if (samlClaims != null && samlClaims?.Count() > 0)
            {
                var claimsOAuthDownLogic = serviceProvider.GetService<ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
                var jwtClaims = await claimsOAuthDownLogic.FromSamlToJwtClaimsAsync(samlClaims);
                return jwtClaims.Where(c => c.Type == JwtClaimTypes.Subject || c.Type == Constants.JwtClaimTypes.SubFormat).ToClaimAndValues();
            }
            return null;
        }

        public async Task<string> CreateOrUpdateSessionAsync<T>(T upParty, List<Claim> claims, string externalSessionId, string idToken = null) where T : UpParty
        {
            logger.ScopeTrace(() => $"Create or update session for authentication method, Route '{RouteBinding.Route}'.");

            var sessionClaims = FilterClaims(claims);

            Action<SessionUpPartyCookie> updateAction = async (session) =>
            {
                if (!sessionClaims.Where(c => c.Type == JwtClaimTypes.SessionId).Any())
                {
                    sessionClaims.AddClaim(JwtClaimTypes.SessionId, await GetSessionIdAsync(upParty));
                }
                session.Claims = sessionClaims.ToClaimAndValues();

                session.ExternalSessionId = externalSessionId;
                try
                {
                    if (idToken?.Count() > Constants.Models.Claim.ValueLength)
                    {
                        throw new Exception($"The ID Token exceeds the maximum allowed limit of {Constants.Models.Claim.ValueLength} bytes and is NOT included in the authentication method session. Logout may not work without the ID Token.");
                    }
                    session.IdToken = idToken;
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                }
            };

            var sessionEnabled = SessionEnabled(upParty);
            var session = await sessionCookieRepository.GetAsync(upParty);
            if (session != null && session.Claims?.Count() > 0)
            {
                var sessionValid = SessionValid(session, upParty);
                var activeSessionExists = sessionValid && await ActiveSessionExistsAsync(session.Claims);

                logger.ScopeTrace(() => $"User id '{session.UserIdClaim}' session for authentication method exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Session id '{session.SessionIdClaim}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid && activeSessionExists)
                {
                    if (session.IsMarkerSession)
                    {
                        updateAction(session);
                        session.IsMarkerSession = false;
                    }

                    var userId = sessionClaims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject);
                    if (!session.UserIdClaim.IsNullOrEmpty() && session.UserIdClaim != userId)
                    {
                        logger.Event($"Existing session user '{session.UserIdClaim}' and authenticated user '{userId}' do not match, causing an session update including new session ID.");
                        updateAction(session);
                    }

                    if (session.ExternalSessionId != externalSessionId)
                    {
                        logger.Event("External session ID has changed, causing an session update including new session ID.");
                        updateAction(session);
                    }

                    await AddOrUpdateSessionTrackWithClaimsAsync(upParty, session.Claims, updateDbActiveSession: false);
                    session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await sessionCookieRepository.SaveAsync(upParty, session, null);
                    logger.ScopeTrace(() => $"Session updated authentication method, Session id '{session.SessionIdClaim}'.", GetSessionScopeProperties(session));

                    return session.SessionIdClaim;
                }

                SetScopeProperty(session, includeSessionId: false);
                if (!sessionEnabled || !activeSessionExists)
                {
                    await sessionCookieRepository.DeleteAsync(upParty);
                    logger.ScopeTrace(() => $"Session deleted, Session id '{session.SessionIdClaim}'.");
                }
            }

            if (sessionEnabled)
            {
                logger.ScopeTrace(() => $"Create session for authentication method, External Session id '{externalSessionId}', Route '{RouteBinding.Route}'.");
                session = new SessionUpPartyCookie();
                updateAction(session);
                session.LastUpdated = session.CreateTime;

                await AddOrUpdateSessionTrackWithClaimsAsync(upParty, session.Claims, updateDbActiveSession: true);
                await sessionCookieRepository.SaveAsync(upParty, session, null);
                logger.ScopeTrace(() => $"Session for authentication method created, User id '{session.UserIdClaim}', Session id '{session.SessionIdClaim}', External Session id '{externalSessionId}'.", GetSessionScopeProperties(session));

                return session.SessionIdClaim;
            }
            else
            {
                logger.SetUserScopeProperty(sessionClaims);
            }

            return null;
        }

        private List<Claim> FilterClaims(List<Claim> claims)
        {
            claims = claims ?? new List<Claim>();
            return claims.Where(c => c.Type == JwtClaimTypes.Subject || c.Type == Constants.JwtClaimTypes.SubFormat || c.Type == JwtClaimTypes.Email || c.Type == JwtClaimTypes.Amr).ToList();
        }

        public async Task<SessionUpPartyCookie> GetSessionAsync<T>(T upParty) where T : UpParty
        {
            logger.ScopeTrace(() => $"Get session for authentication method, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync(upParty);
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(upParty);
                var sessionValid = SessionValid(session, upParty);
                var activeSessionExists = sessionValid && await ActiveSessionExistsAsync(session.Claims);

                logger.ScopeTrace(() => $"User id '{session.UserIdClaim}' session for authentication method exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Active '{activeSessionExists}', Session id '{session.SessionIdClaim}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid && activeSessionExists)
                {
                    SetScopeProperty(session);
                    return session;
                }

                SetScopeProperty(session, includeSessionId: false);
                await sessionCookieRepository.DeleteAsync(upParty);
                logger.ScopeTrace(() => $"Session deleted for authentication method, Session id '{session.SessionIdClaim}'.");
            }
            else
            {
                logger.ScopeTrace(() => $"Session for authentication method '{upParty.Name}' do not exists.", triggerEvent: true);
            }

            return null;
        }

        public async Task<SessionUpPartyCookie> DeleteSessionAsync<T>(T upParty, SessionUpPartyCookie session = null) where T : UpParty
        {
            logger.ScopeTrace(() => $"Delete session for authentication method, Route '{RouteBinding.Route}'.");
            session = session ?? await sessionCookieRepository.GetAsync(upParty);
            if (session != null)
            {
                await sessionCookieRepository.DeleteAsync(upParty);
                logger.ScopeTrace(() => $"Session for deleted authentication method, Session id '{session.SessionIdClaim}'.");
                return session;
            }
            else
            {
                logger.ScopeTrace(() => "Session for authentication method do not exists.");
                return null;
            }
        }

        protected IDictionary<string, string> GetSessionScopeProperties(SessionUpPartyCookie session, bool includeSessionId = true)
        {
            var scopeProperties = GetSessionScopeProperties(session as SessionBaseCookie, includeSessionId: includeSessionId);
            if (includeSessionId)
            { 
                scopeProperties.Add(Constants.Logs.ExternalSessionId, session.ExternalSessionId);
            }
            return scopeProperties;
        }

        private void SetScopeProperty(SessionUpPartyCookie session, bool includeSessionId = true)
        {
            var scopeProperties = GetSessionScopeProperties(session, includeSessionId: includeSessionId);
            foreach(var p in scopeProperties)
            {
                logger.SetScopeProperty(p.Key, p.Value);
            }
        }
    }
}
