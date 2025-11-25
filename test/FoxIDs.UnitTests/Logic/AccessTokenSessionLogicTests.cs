using FoxIDs;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using ITfoxtec.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests.Logic
{
    public class ActiveSessionLogicTests
    {
        [Fact]
        public async Task SaveSessionAsync_WithSessionId_SavesSessionWithMetadata()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            ActiveSessionTtl savedSession = null;

            tenantDataRepositoryMock.Setup(r => r.SaveAsync(It.IsAny<ActiveSessionTtl>(), It.IsAny<TelemetryScopedLogger>()))
                .Callback<ActiveSessionTtl, TelemetryScopedLogger>((session, _) => savedSession = session)
                .Returns(ValueTask.CompletedTask);

            var sessionId = CreateSessionId("sid123");
            var downPartyLink = new DownPartySessionLink { Id = CreatePartyId(routeBinding, "app1"), Type = PartyTypes.Oidc };
            var claims = new[]
            {
                new Claim(JwtClaimTypes.SessionId, sessionId),
                new Claim(JwtClaimTypes.Subject, "sub1"),
                new Claim(Constants.JwtClaimTypes.SubFormat, "sub-format"),
                new Claim(JwtClaimTypes.Email, "user@example.com"),
                new Claim(JwtClaimTypes.PhoneNumber, "12345678"),
                new Claim(JwtClaimTypes.PreferredUsername, "user1")
            };

            await logic.SaveSessionAsync(downPartyLink, claims);

            tenantDataRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ActiveSessionTtl>(), It.IsAny<TelemetryScopedLogger>()), Times.Once);
            Assert.NotNull(savedSession);
            var sessionIdHash = await sessionId.HashIdStringAsync();
            var expectedId = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = sessionIdHash });
            Assert.Equal(expectedId, savedSession.Id);
            Assert.Equal(sessionId, savedSession.SessionId);
            Assert.Equal(ActiveSessionTtl.DefaultTimeToLive, savedSession.TimeToLive);
            Assert.Equal("sub1", savedSession.Sub);
            Assert.Equal("sub-format", savedSession.SubFormat);
            Assert.Equal("user@example.com", savedSession.Email);
            Assert.Equal("12345678", savedSession.Phone);
            Assert.Equal("user1", savedSession.Username);
            Assert.Single(savedSession.DownPartyLinks);
            var downLink = savedSession.DownPartyLinks.Single();
            Assert.Equal("app1", downLink.Name);
            Assert.Equal(PartyTypes.Oidc, downLink.Type);
            Assert.True(savedSession.CreateTime > 0);
        }

        [Fact]
        public async Task SaveSessionAsync_WithSessionGroups_SavesEachSession()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var savedSessions = new List<ActiveSessionTtl>();

            tenantDataRepositoryMock.Setup(r => r.SaveAsync(It.IsAny<ActiveSessionTtl>(), It.IsAny<TelemetryScopedLogger>()))
                .Callback<ActiveSessionTtl, TelemetryScopedLogger>((session, _) => savedSessions.Add(session))
                .Returns(ValueTask.CompletedTask);

            var sessionOneId = CreateSessionId("sid1");
            var sessionTwoId = CreateSessionId("sid2");
            var sessionGroups = new List<SessionTrackCookieGroup>
            {
                CreateSessionGroup(routeBinding, sessionOneId, "login1", "app1"),
                CreateSessionGroup(routeBinding, sessionTwoId, "login2", "app2")
            };

            await logic.SaveSessionAsync(sessionGroups, createTime: 1234, lastUpdated: 5678);

            Assert.Equal(2, savedSessions.Count);
            Assert.All(savedSessions, session =>
            {
                Assert.Equal(ActiveSessionTtl.DefaultTimeToLive, session.TimeToLive);
                Assert.Equal(1234, session.CreateTime);
                Assert.Equal(5678, session.LastUpdated);
                Assert.NotEmpty(session.UpPartyLinks);
                Assert.NotNull(session.SessionUpParty);
                Assert.NotEmpty(session.DownPartyLinks);
            });
            var storedIds = savedSessions.Select(s => s.SessionId).ToHashSet();
            Assert.Contains(sessionOneId, storedIds);
            Assert.Contains(sessionTwoId, storedIds);
        }

        [Fact]
        public async Task ValidateSessionAsync_WhenSessionMissing_ThrowsSessionException()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var sessionId = CreateSessionId("sid123");
            var sessionIdHash = await sessionId.HashIdStringAsync();
            var expectedId = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = sessionIdHash });

            tenantDataRepositoryMock.Setup(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, false, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync((ActiveSessionTtl)null);

            await Assert.ThrowsAsync<SessionException>(() => logic.ValidateSessionAsync(new[] { new Claim(JwtClaimTypes.SessionId, sessionId) }));
        }

        [Fact]
        public async Task ValidateSessionAsync_WhenSessionExists_DoesNotThrow()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var sessionId = CreateSessionId("sid123");
            var sessionIdHash = await sessionId.HashIdStringAsync();
            var expectedId = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = sessionIdHash });

            tenantDataRepositoryMock.Setup(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, false, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync(new ActiveSessionTtl { Id = expectedId, SessionId = sessionId });

            await logic.ValidateSessionAsync(new[] { new Claim(JwtClaimTypes.SessionId, sessionId) });

            tenantDataRepositoryMock.Verify(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, false, false, It.IsAny<TelemetryScopedLogger>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSessionAsync_DeletesStoredSession()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var sessionId = CreateSessionId("sid123");
            var sessionIdHash = await sessionId.HashIdStringAsync();
            var expectedId = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = sessionIdHash });

            tenantDataRepositoryMock.Setup(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, true, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync(new ActiveSessionTtl { Id = expectedId, SessionId = sessionId, TimeToLive = ActiveSessionTtl.DefaultTimeToLive });

            await logic.DeleteSessionAsync(sessionId);

            tenantDataRepositoryMock.Verify(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, true, false, It.IsAny<TelemetryScopedLogger>()), Times.Once);
        }

        [Fact]
        public async Task ListSessionsAsync_UsesFilter()
        {
            var (logic, tenantDataRepositoryMock, _) = CreateLogic();
            Expression<Func<ActiveSessionTtl, bool>> filter = null;

            tenantDataRepositoryMock.Setup(r => r.GetManyAsync(It.IsAny<Track.IdKey>(), It.IsAny<Expression<Func<ActiveSessionTtl, bool>>>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TelemetryScopedLogger>()))
                .Callback<Track.IdKey, Expression<Func<ActiveSessionTtl, bool>>, int, string, TelemetryScopedLogger>((_, f, __, ___, ____) => filter = f)
                .ReturnsAsync(((IReadOnlyCollection<ActiveSessionTtl>)new List<ActiveSessionTtl>(), "token1"));

            var sessionId = CreateSessionId("sid123");
            await logic.ListSessionsAsync("user@example.com", "sub1", "login", "app1", sessionId);

            Assert.NotNull(filter);
            var matches = filter.Compile()(new ActiveSessionTtl
            {
                DataType = Constants.Models.DataType.ActiveSession,
                Email = "user@example.com",
                Sub = "sub1",
                UpPartyLinks = new List<PartyNameSessionLink> { new PartyNameSessionLink { Name = "login", Type = PartyTypes.Login } },
                DownPartyLinks = new List<PartyNameSessionLink> { new PartyNameSessionLink { Name = "app1", Type = PartyTypes.Oidc } },
                SessionId = sessionId
            });
            Assert.True(matches);
        }

        [Fact]
        public async Task DeleteSessionsAsync_UsesQueryWithSessionId()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            Track.IdKey usedIdKey = null;
            Expression<Func<ActiveSessionTtl, bool>> filter = null;

            tenantDataRepositoryMock.Setup(r => r.DeleteManyAsync(It.IsAny<Track.IdKey>(), It.IsAny<Expression<Func<ActiveSessionTtl, bool>>>(), It.IsAny<TelemetryScopedLogger>()))
                .Callback<Track.IdKey, Expression<Func<ActiveSessionTtl, bool>>, TelemetryScopedLogger>((idKey, f, _) =>
                {
                    usedIdKey = idKey;
                    filter = f;
                })
                .ReturnsAsync(1);

            var sessionId = CreateSessionId("sid123");
            await logic.DeleteSessionsAsync("user@example.com", downPartyName: "app1", sessionId: sessionId);

            Assert.Equal(routeBinding.TenantName, usedIdKey.TenantName);
            Assert.Equal(routeBinding.TrackName, usedIdKey.TrackName);
            var matches = filter.Compile()(new ActiveSessionTtl
            {
                DataType = Constants.Models.DataType.ActiveSession,
                Email = "user@example.com",
                DownPartyLinks = new List<PartyNameSessionLink> { new PartyNameSessionLink { Name = "app1", Type = PartyTypes.Oidc } },
                SessionId = sessionId
            });
            Assert.True(matches);
        }

        private static (ActiveSessionLogic logic, Mock<ITenantDataRepository> repositoryMock, RouteBinding routeBinding) CreateLogic()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant1", TrackName = "track1" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var telemetryScopedLogger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);
            var repositoryMock = new Mock<ITenantDataRepository>();

            var logic = new ActiveSessionLogic(telemetryScopedLogger, repositoryMock.Object, httpContextAccessor);
            return (logic, repositoryMock, routeBinding);
        }

        private static SessionTrackCookieGroup CreateSessionGroup(RouteBinding routeBinding, string sessionId, string upPartyName, string downPartyName) => new SessionTrackCookieGroup
        {
            Claims = new List<ClaimAndValues>
            {
                new ClaimAndValues { Claim = JwtClaimTypes.SessionId, Values = new List<string> { sessionId } },
                new ClaimAndValues { Claim = JwtClaimTypes.Subject, Values = new List<string> { "sub1" } },
                new ClaimAndValues { Claim = JwtClaimTypes.Email, Values = new List<string> { $"{sessionId}@example.com" } }
            },
            UpPartyLinks = new List<UpPartySessionLink> { new UpPartySessionLink { Id = CreatePartyId(routeBinding, upPartyName), Type = PartyTypes.Login } },
            SessionUpParty = new UpPartySessionLink { Id = CreatePartyId(routeBinding, upPartyName), Type = PartyTypes.Login },
            DownPartyLinks = new List<DownPartySessionLink> { new DownPartySessionLink { Id = CreatePartyId(routeBinding, downPartyName), Type = PartyTypes.Oidc } }
        };

        private static string CreatePartyId(RouteBinding routeBinding, string partyName) => $"{routeBinding.TenantName}:{routeBinding.TrackName}:{partyName}";

        private static string CreateSessionId(string baseValue) => $"{baseValue}{Constants.Models.Session.ShortSessionPostKey}";
    }
}
