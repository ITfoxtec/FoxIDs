using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ActiveSessionLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public ActiveSessionLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task SaveSessionAsync(DownPartySessionLink downPartyLink, IEnumerable<Claim> claims)
        {
            var sessionId = GetSessionId(claims);
            if (sessionId.IsNullOrWhiteSpace())
            {
               return;
            }

            var sessionIdHash = await sessionId.HashIdStringAsync();
            var session = await GetExistingSessionAsync(sessionIdHash);
            if (session == null)
            {
                await CreateActiveSessionAsync(sessionId, sessionIdHash, downPartyLink, claims);
                logger.ScopeTrace(() => $"Created active session, Route '{RouteBinding.Route}', Session ID '{sessionId}'.");
            }
            else
            {
                await UpdateActiveSessionAsync(session, downPartyLink, claims);
                logger.ScopeTrace(() => $"Updated active session, Route '{RouteBinding.Route}', Session ID '{sessionId}'.");
            }

        }

        public async Task SaveSessionAsync(IEnumerable<SessionTrackCookieGroup> sessionTrackCookieGroups, long createTime, long lastUpdated)
        {
            foreach (var sessionGroup in sessionTrackCookieGroups)
            {
                var sessionId = GetSessionId(sessionGroup.Claims);
                if (sessionId.IsNullOrWhiteSpace())
                {
                    break;
                }

                await SaveActiveSessionAsync(sessionId, sessionGroup, createTime, lastUpdated);
                logger.ScopeTrace(() => $"Saved active session, Route '{RouteBinding.Route}', Session ID '{sessionId}'.");
            }
        }

        private async Task<ActiveSessionTtl> GetExistingSessionAsync(string sessionIdHash, string trackName = null)
        {
            var id = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = trackName ?? RouteBinding.TrackName, SessionIdHash = sessionIdHash });
            return await tenantDataRepository.GetAsync<ActiveSessionTtl>(id, required: false);
        }

        private async Task CreateActiveSessionAsync(string sessionId, string sessionIdHash, DownPartySessionLink downPartyLink, IEnumerable<Claim> claims)
        {
            var session = new ActiveSessionTtl 
            {
                TimeToLive = ActiveSessionTtl.DefaultTimeToLive,
                SessionId = sessionId 
            };
            await session.SetIdAsync(new ActiveSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, SessionIdHash = sessionIdHash });
            session.CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ApplyClaimValues(session, claims);
            ApplyDownPartyLink(session, [downPartyLink]);
            ApplyClientIpAndUserAgent(session);
            await tenantDataRepository.SaveAsync(session);
        }

        private async Task UpdateActiveSessionAsync(ActiveSessionTtl session, DownPartySessionLink downPartyLink, IEnumerable<Claim> claims)
        {
            session.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ApplyClaimValues(session, claims);
            ApplyClientIpAndUserAgent(session);
            await tenantDataRepository.SaveAsync(session);
        }

        private async Task SaveActiveSessionAsync(string sessionId, SessionTrackCookieGroup sessionGroup, long createTime, long lastUpdated)
        {
            var session = new ActiveSessionTtl
            {
                TimeToLive = ActiveSessionTtl.DefaultTimeToLive,
                SessionId = sessionId
            };
            await session.SetIdAsync(new ActiveSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, SessionIdHash = await sessionId.HashIdStringAsync() });
            session.CreateTime = createTime;
            session.LastUpdated = lastUpdated;
            ApplyClaimValues(session, sessionGroup.Claims);
            ApplyUpPartyLink(session, sessionGroup.UpPartyLinks);
            ApplySessionUpParty(session, sessionGroup.SessionUpParty);
            ApplyDownPartyLink(session, sessionGroup.DownPartyLinks);
            ApplyClientIpAndUserAgent(session);
            await tenantDataRepository.SaveAsync(session);
        }

        private void ApplyClaimValues(ActiveSessionTtl session, IEnumerable<Claim> claims)
        {
            var sub = GetTruncatedClaimValue(claims, JwtClaimTypes.Subject);
            if (!sub.IsNullOrWhiteSpace())
            {
                session.Sub = sub;
            }

            var subFormat = GetTruncatedClaimValue(claims, Constants.JwtClaimTypes.SubFormat);
            if (!subFormat.IsNullOrWhiteSpace())
            {
                session.SubFormat = subFormat;
            }

            var email = GetTruncatedClaimValue(claims, JwtClaimTypes.Email);
            if (!email.IsNullOrWhiteSpace())
            {
                session.Email = email;
            }

            var phone = GetTruncatedClaimValue(claims, JwtClaimTypes.PhoneNumber);
            if (!phone.IsNullOrWhiteSpace())
            {
                session.Phone = phone;
            }

            var username = GetTruncatedClaimValue(claims, JwtClaimTypes.PreferredUsername);
            if (!username.IsNullOrWhiteSpace() && (email.IsNullOrWhiteSpace() || username != email))
            {
                session.Username = username;
            }
        }

        private void ApplyClaimValues(ActiveSessionTtl session, IEnumerable<ClaimAndValues> claims)
        {
            var sub = GetTruncatedClaimValue(claims, JwtClaimTypes.Subject);
            if (!sub.IsNullOrWhiteSpace())
            {
                session.Sub = sub;
            }

            var subFormat = GetTruncatedClaimValue(claims, Constants.JwtClaimTypes.SubFormat);
            if (!subFormat.IsNullOrWhiteSpace())
            {
                session.SubFormat = subFormat;
            }

            var email = GetTruncatedClaimValue(claims, JwtClaimTypes.Email);
            if (!email.IsNullOrWhiteSpace())
            {
                session.Email = email;
            }

            var phone = GetTruncatedClaimValue(claims, JwtClaimTypes.PhoneNumber);
            if (!phone.IsNullOrWhiteSpace())
            {
                session.Phone = phone;
            }

            var username = GetTruncatedClaimValue(claims, JwtClaimTypes.PreferredUsername);
            if (!username.IsNullOrWhiteSpace() && (email.IsNullOrWhiteSpace() || username != email))
            {
                session.Username = username;
            }
        }

        private void ApplyUpPartyLink(ActiveSessionTtl session, List<UpPartySessionLink> upPartyLinks)
        {
            if (session.UpPartyLinks == null)
            {
                session.UpPartyLinks = new List<PartyNameSessionLink>();
            }
            foreach (var upPartyLink in upPartyLinks)
            {
                var partyName = upPartyLink.Id.PartyIdToName(); 
                if (session.UpPartyLinks.Count < Constants.Models.Session.LinksMax && !session.UpPartyLinks.Any(u => u.Name == partyName))
                {
                    session.UpPartyLinks.Add(new PartyNameSessionLink { Name = partyName, Type = upPartyLink.Type });
                }
            }
        }

        private void ApplySessionUpParty(ActiveSessionTtl session, UpPartySessionLink sessionUpParty)
        {
            session.SessionUpParty = new PartyNameSessionLink { Name = sessionUpParty.Id.PartyIdToName(), Type = sessionUpParty.Type };
        }

        private void ApplyDownPartyLink(ActiveSessionTtl session, IEnumerable<DownPartySessionLink> downPartyLinks)
        {
            if (session.DownPartyLinks == null)
            {
                session.DownPartyLinks = new List<PartyNameSessionLink>();
            }
            foreach (var downPartyLink in downPartyLinks)
            {
                var partyName = downPartyLink.Id.PartyIdToName();
                if (session.DownPartyLinks.Count < Constants.Models.Session.LinksMax && !session.DownPartyLinks.Any(d => d.Name == partyName))
                {
                    session.DownPartyLinks.Add(new PartyNameSessionLink { Name = partyName, Type = downPartyLink.Type });
                }
            }
        }

        private void ApplyClientIpAndUserAgent(ActiveSessionTtl session)
        {        
            var clientIp = GetTruncatedValue(HttpContext.Connection.RemoteIpAddress.ToString());
            if (!clientIp.IsNullOrWhiteSpace())
            {
                session.ClientIp = clientIp;
            }

            var userAgent = GetTruncatedValue(HttpContext.Request.Headers["User-Agent"]);
            if (!userAgent.IsNullOrWhiteSpace())
            {
                session.UserAgent = userAgent;
            }
        }

        public async Task ValidateSessionAsync(IEnumerable<Claim> claims, string trackName = null)
        {
            var sessionId = GetSessionId(claims);
            if (sessionId.IsNullOrWhiteSpace())
            {
                return;
            }

            var sessionIdHash = await sessionId.HashIdStringAsync();

            var session = await GetExistingSessionAsync(sessionIdHash, trackName);
            if (session != null)
            {
                return;
            }

            throw new SessionException($"Active session for session ID '{sessionId}' does not exist.");
        }

        public async Task<(IReadOnlyCollection<ActiveSessionTtl> sessions, string paginationToken)> ListSessionsAsync(string userIdentifier, string sub, string sessionId, string downPartyName, string upPartyName, string paginationToken = null)
        {
            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            return await tenantDataRepository.GetManyAsync(idKey, GetQuery(userIdentifier, sub, sessionId, downPartyName, upPartyName, null), paginationToken: paginationToken);
        }

        public async Task DeleteSessionsAsync(string userIdentifier, string sub = null, string sessionId = null, string downPartyName = null, string upPartyName = null, PartyTypes ? upPartyType = null)
        {
            logger.ScopeTrace(() => $"Delete active sessions, Route '{RouteBinding.Route}', User identifier '{userIdentifier}', Sub '{sub}', Session ID '{sessionId}', application '{downPartyName}', Auth method '{upPartyName}', Auth method type '{upPartyType}'.");

            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            var deletedCount = await tenantDataRepository.DeleteManyAsync(idKey, GetQuery(userIdentifier, sub, sessionId, downPartyName, upPartyName, upPartyType));
            if (deletedCount > 0)
            {
                logger.ScopeTrace(() => $"Access token sessions deleted.");
            }
        }

        public Task DeleteSessionsByEmailAsync(string email, PartyTypes upPartyType = PartyTypes.Login)
        {
            if (email.IsNullOrWhiteSpace()) return Task.CompletedTask;
            return DeleteSessionsAsync(email, upPartyType: upPartyType);
        }

        public Task DeleteSessionsByPhoneAsync(string phone, PartyTypes upPartyType = PartyTypes.Login)
        {
            if (phone.IsNullOrWhiteSpace()) return Task.CompletedTask;
            return DeleteSessionsAsync(phone, upPartyType: upPartyType);
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            if (sessionId.EndsWith(Constants.Models.Session.ShortSessionPostKey, StringComparison.Ordinal))
            {
                var id = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, SessionIdHash = await sessionId.HashIdStringAsync() });
                var session = await tenantDataRepository.GetAsync<ActiveSessionTtl>(id, required: false, delete: true);
                if (session != null)
                {
                    logger.ScopeTrace(() => $"Active session deleted, Route '{RouteBinding.Route}', Session ID '{sessionId}'.");
                }
            }
        }

        private string GetSessionId(IEnumerable<Claim> claims)
        {
            var sessionId = claims?.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.SessionId);
            return GetShortSessionId(sessionId);
        }

        private string GetSessionId(IEnumerable<ClaimAndValues> claims)
        {
            var sessionId = claims?.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.SessionId);
            return GetShortSessionId(sessionId);
        }

        private static string GetShortSessionId(string sessionId)
        {
            if (!sessionId.IsNullOrWhiteSpace() && sessionId.EndsWith(Constants.Models.Session.ShortSessionPostKey, StringComparison.Ordinal))
            {
                return sessionId;
            }
            return null;
        }

        private string GetTruncatedValue(string value)
        {
            if (value?.Length > Constants.Models.Claim.ValueLength)
            {
                return value.Substring(0, Constants.Models.Claim.ValueLength);
            }
            return value;
        }

        private string GetTruncatedClaimValue(IEnumerable<Claim> claims, string claimType)
        {
            var value = claims?.FindFirstOrDefaultValue(c => c.Type == claimType);
            return GetTruncatedValue(value);
        }

        private string GetTruncatedClaimValue(IEnumerable<ClaimAndValues> claims, string claimType)
        {
            var value = claims?.FindFirstOrDefaultValue(c => c.Claim == claimType);
            return GetTruncatedValue(value);
        }

        private static Expression<Func<ActiveSessionTtl, bool>> GetQuery(string userIdentifier, string sub, string sessionId, string downPartyName, string upPartyName, PartyTypes? upPartyType)
        {
            var queryByUserIdentifier = !userIdentifier.IsNullOrWhiteSpace();
            var queryBySub = !sub.IsNullOrWhiteSpace();
            var queryBySessionId = !sessionId.IsNullOrWhiteSpace();
            var queryByDownPartyName = !downPartyName.IsNullOrWhiteSpace();
            var queryByUpPartyName = !upPartyName.IsNullOrWhiteSpace();
            var queryByUpPartyType = upPartyType.HasValue;

            return s => s.DataType.Equals(Constants.Models.DataType.ActiveSession) &&
                (
                    (!queryByUserIdentifier && !queryBySub) || // none provided
                    (queryByUserIdentifier && !queryBySub && (s.Email == userIdentifier || s.Phone == userIdentifier || s.Username == userIdentifier)) || // only userIdentifier
                    (!queryByUserIdentifier && queryBySub && s.Sub == sub) || // only sub
                    (queryByUserIdentifier && queryBySub && (s.Email == userIdentifier || s.Phone == userIdentifier || s.Username == userIdentifier || s.Sub == sub)) // both => OR
                ) &&
                (!queryBySessionId || s.SessionId == sessionId) &&
                (!queryByDownPartyName || (s.DownPartyLinks != null && s.DownPartyLinks.Any(d => d.Name == downPartyName))) &&
                (!queryByUpPartyName || (s.UpPartyLinks != null && s.UpPartyLinks.Any(u => u.Name == upPartyName))) &&
                (!queryByUpPartyType || (s.UpPartyLinks != null && s.UpPartyLinks.Any(u => u.Type == upPartyType)));;
        }
    }
}
