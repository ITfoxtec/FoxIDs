﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Session;
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
    public class SessionLoginUpPartyLogic : SessionBaseLogic
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly UpPartyCookieRepository<SessionLoginUpPartyCookie> sessionCookieRepository;

        public SessionLoginUpPartyLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, UpPartyCookieRepository<SessionLoginUpPartyCookie> sessionCookieRepository, IHttpContextAccessor httpContextAccessor) : base(settings, httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.sessionCookieRepository = sessionCookieRepository;
        }

        public async Task CreateSessionAsync(IUpParty loginUpParty, DownPartySessionLink newDownPartyLink, long authTime, LoginUserIdentifier loginUserIdentifier, IEnumerable<Claim> claims)
        {
            if(SessionEnabled(loginUpParty))
            {
                logger.ScopeTrace(() => $"Create session, Route '{RouteBinding.Route}'.");

                var session = new SessionLoginUpPartyCookie
                {
                    Claims = claims.ToClaimAndValues(),
                };
                AddDownPartyLink(session, newDownPartyLink);
                session.CreateTime = authTime;
                session.LastUpdated = authTime;
                SetLoginUserIdentifier(session, loginUserIdentifier);
                await sessionCookieRepository.SaveAsync(loginUpParty, session, GetPersistentCookieExpires(loginUpParty, session.CreateTime));
                logger.ScopeTrace(() => $"Session created, User id '{session.UserIdClaim}', Session id '{session.SessionIdClaim}'.", GetSessionScopeProperties(session));
            }
            else
            {
                logger.SetUserScopeProperty(claims);
            }
        }

        public async Task<bool> UpdateSessionAsync(IUpParty upParty, DownPartySessionLink newDownPartyLink, SessionLoginUpPartyCookie session, LoginUserIdentifier loginUserIdentifier = null, IEnumerable<Claim> claims = null)
        {
            logger.ScopeTrace(() => $"Update session, Route '{RouteBinding.Route}'.");

            var sessionEnabled = SessionEnabled(upParty);
            var sessionValid = SessionValid(session, upParty);

            if (sessionEnabled && sessionValid)
            {
                AddDownPartyLink(session, newDownPartyLink);
                session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (claims?.Count() > 0)
                {
                    session.Claims = UpdateClaims(session, claims);
                }
                if (loginUserIdentifier != null)
                {
                    SetLoginUserIdentifier(session, loginUserIdentifier);
                }
                await sessionCookieRepository.SaveAsync(upParty, session, GetPersistentCookieExpires(upParty, session.CreateTime));
                logger.ScopeTrace(() => $"Session updated, Session id '{session.SessionIdClaim}'.", GetSessionScopeProperties(session));
                return true;
            }

            SetScopeProperty(session, includeSessionId: false);
            await sessionCookieRepository.DeleteAsync(upParty);
            logger.ScopeTrace(() => $"Session deleted, Session id '{session.SessionIdClaim}'.");
            return false;
        }

        private void SetLoginUserIdentifier(SessionLoginUpPartyCookie session, LoginUserIdentifier loginUserIdentifier)
        {
            session.UserId = loginUserIdentifier.UserId;
            session.Email = loginUserIdentifier.Email;
            session.Phone = loginUserIdentifier.Phone;
            session.Username = loginUserIdentifier.Username;
            session.UserIdentifier = loginUserIdentifier.UserIdentifier;
        }

        private IEnumerable<ClaimAndValues> UpdateClaims(SessionLoginUpPartyCookie session, IEnumerable<Claim> claims)
        {
            var sessionClaims = new List<ClaimAndValues>(session.Claims);
            var addClaims = claims.ToClaimAndValues();
            foreach(var addClaim in addClaims)
            {
                var claim = sessionClaims.Where(c => c.Claim == addClaim.Claim).FirstOrDefault();
                if (claim == null)
                {
                    sessionClaims.Add(addClaim);
                }
                else
                {
                    foreach(var addValue in addClaim.Values)
                    {
                        if (!claim.Values.Where(v => v == addValue).Any())
                        {
                            claim.Values.Add(addValue);
                        }
                    }
                }
            }

            return sessionClaims;
        }

        public async Task<(SessionLoginUpPartyCookie, User)> GetAndUpdateSessionCheckUserAsync(LoginUpParty loginUpParty, DownPartySessionLink newDownPartyLink)
        {
            logger.ScopeTrace(() => $"Get and update session and check user, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync(loginUpParty);
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(loginUpParty);
                var sessionValid = SessionValid(session, loginUpParty);

                logger.ScopeTrace(() => $"User id '{session.UserIdClaim}' session exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Session id '{session.SessionIdClaim}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    var email = session.Email;
                    if (session.Email.IsNullOrEmpty() && session.UserIdentifier.IsNullOrEmpty() && session.UserId.IsNullOrEmpty())
                    {
                        // For backwards before version 1.15.0 - January 2025, can be deleted in about a year from now.
                        email = session.EmailClaim;
                    }
                    var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email, UserIdentifier = session.UserIdentifier, UserId = session.UserId });
                    var user = await tenantDataRepository.GetAsync<User>(id, false);
                    if (user != null && !user.DisableAccount)
                    {
                        AddDownPartyLink(session, newDownPartyLink);
                        session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        await sessionCookieRepository.SaveAsync(loginUpParty, session, GetPersistentCookieExpires(loginUpParty, session.CreateTime));
                        logger.ScopeTrace(() => $"Session updated, Session id '{session.SessionIdClaim}'.", GetSessionScopeProperties(session));
                        return (session, user);
                    }
                }

                SetScopeProperty(session, includeSessionId: false);
                await sessionCookieRepository.DeleteAsync(loginUpParty);
                logger.ScopeTrace(() => $"Session deleted, Session id '{session.SessionIdClaim}'.");
            }
            else
            {                
                logger.ScopeTrace(() => "Session do not exists.");
            }

            return (null, null);
        }

        public async Task<SessionLoginUpPartyCookie> GetAndUpdateExternalSessionAsync(ExternalLoginUpParty extLoginUpParty, DownPartySessionLink newDownPartyLink)
        {
            logger.ScopeTrace(() => $"Get and update external session, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync(extLoginUpParty);
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(extLoginUpParty);
                var sessionValid = SessionValid(session, extLoginUpParty);

                logger.ScopeTrace(() => $"User id '{session.UserIdClaim}' session (external login) exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Session id '{session.SessionIdClaim}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    AddDownPartyLink(session, newDownPartyLink);
                    session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await sessionCookieRepository.SaveAsync(extLoginUpParty, session, GetPersistentCookieExpires(extLoginUpParty, session.CreateTime));
                    logger.ScopeTrace(() => $"Session (external login) updated, Session id '{session.SessionIdClaim}'.", GetSessionScopeProperties(session));
                    return session;
                }

                SetScopeProperty(session, includeSessionId: false);
                await sessionCookieRepository.DeleteAsync(extLoginUpParty);
                logger.ScopeTrace(() => $"Session (external login) deleted, Session id '{session.SessionIdClaim}'.");
            }
            else
            {
                logger.ScopeTrace(() => "Session (external login) do not exists.");
            }

            return null;
        }

        public async Task<SessionLoginUpPartyCookie> GetSessionAsync(IUpParty upParty)
        {
            logger.ScopeTrace(() => $"Get session, Route '{RouteBinding.Route}'.");

            var session = await sessionCookieRepository.GetAsync(upParty);
            if (session != null)
            {
                var sessionEnabled = SessionEnabled(upParty);
                var sessionValid = SessionValid(session, upParty);

                logger.ScopeTrace(() => $"User id '{session.UserIdClaim}' session exists, Enabled '{sessionEnabled}', Valid '{sessionValid}', Session id '{session.SessionIdClaim}', Route '{RouteBinding.Route}'.");
                if (sessionEnabled && sessionValid)
                {
                    SetScopeProperty(session);
                    return session;
                }

                SetScopeProperty(session, includeSessionId: false);
                await sessionCookieRepository.DeleteAsync(upParty);
                logger.ScopeTrace(() => $"Session deleted, Session id '{session.SessionIdClaim}'.");
            }
            else
            {
                logger.ScopeTrace(() => "Session do not exists.");
            }

            return null;
        }

        public async Task<SessionLoginUpPartyCookie> DeleteSessionAsync(IUpParty upParty)
        {
            logger.ScopeTrace(() => $"Delete session, Route '{RouteBinding.Route}'.");
            var session = await sessionCookieRepository.GetAsync(upParty);
            if (session != null)
            {
                await sessionCookieRepository.DeleteAsync(upParty);
                logger.ScopeTrace(() => $"Session deleted, Session id '{session.SessionIdClaim}'.");
                return session;
            }
            else
            {
                logger.ScopeTrace(() => "Session do not exists.");
                return null;
            }
        }

        private void SetScopeProperty(SessionLoginUpPartyCookie session, bool includeSessionId = true)
        {
            var scopeProperties = GetSessionScopeProperties(session, includeSessionId: includeSessionId);
            foreach (var p in scopeProperties)
            {
                logger.SetScopeProperty(p.Key, p.Value);
            }
        }
    }
}
