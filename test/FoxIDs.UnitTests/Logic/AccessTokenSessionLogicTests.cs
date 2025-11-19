using FoxIDs;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
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

            var claims = new[]
            {
                new Claim(JwtClaimTypes.SessionId, "sid123"),
                new Claim(JwtClaimTypes.Subject, "sub1"),
                new Claim(JwtClaimTypes.Email, "user@example.com"),
                new Claim(JwtClaimTypes.PhoneNumber, "12345678"),
                new Claim(JwtClaimTypes.PreferredUsername, "user1"),
                new Claim(JwtClaimTypes.ClientId, "client1"),
                new Claim(Constants.JwtClaimTypes.AuthMethod, "login"),
                new Claim(Constants.JwtClaimTypes.AuthMethodType, PartyTypes.Login.GetPartyTypeValue())
            };

            await logic.SaveSessionAsync(claims);

            tenantDataRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ActiveSessionTtl>(), It.IsAny<TelemetryScopedLogger>()), Times.Once);
            Assert.NotNull(savedSession);
            var sessionIdHash = await "sid123".HashIdStringAsync();
            var expectedId = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = sessionIdHash });
            Assert.Equal(expectedId, savedSession.Id);
            Assert.Equal(sessionIdHash, savedSession.SessionId);
            Assert.Equal(ActiveSessionTtl.DefaultTimeToLive, savedSession.TimeToLive);
            Assert.Equal("client1", savedSession.ClientId);
            Assert.Equal("sub1", savedSession.Sub);
            Assert.Equal("user@example.com", savedSession.Email);
            Assert.Equal("12345678", savedSession.Phone);
            Assert.Equal("user1", savedSession.Username);
            Assert.Equal("login", savedSession.UpPartyName);
            Assert.Equal(PartyTypes.Login.GetPartyTypeValue(), savedSession.UpPartyType);
            Assert.True(savedSession.CreateTime > 0);
        }

        [Fact]
        public async Task SaveSessionAsync_WithAdditionalSessionIds_SavesAllSessions()
        {
            var (logic, tenantDataRepositoryMock, _) = CreateLogic();
            IReadOnlyCollection<ActiveSessionTtl> savedSessions = null;

            tenantDataRepositoryMock.Setup(r => r.SaveManyAsync(It.IsAny<IReadOnlyCollection<ActiveSessionTtl>>(), It.IsAny<TelemetryScopedLogger>()))
                .Callback<IReadOnlyCollection<ActiveSessionTtl>, TelemetryScopedLogger>((sessions, _) => savedSessions = sessions)
                .Returns(ValueTask.CompletedTask);

            await logic.SaveSessionAsync([new Claim(JwtClaimTypes.Subject, "sub1")], ["sid1", "sid2"]);

            tenantDataRepositoryMock.Verify(r => r.SaveManyAsync(It.IsAny<IReadOnlyCollection<ActiveSessionTtl>>(), It.IsAny<TelemetryScopedLogger>()), Times.Once);
            Assert.NotNull(savedSessions);
            Assert.Equal(2, savedSessions.Count);
            var expectedHashes = new HashSet<string> { await "sid1".HashIdStringAsync(), await "sid2".HashIdStringAsync() };
            Assert.Equal(expectedHashes, savedSessions.Select(s => s.SessionId).ToHashSet());
        }

        [Fact]
        public async Task ValidateSessionAsync_WhenSessionMissing_ThrowsSessionException()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var sessionIdHash = await "sid123".HashIdStringAsync();
            var expectedId = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = sessionIdHash });

            tenantDataRepositoryMock.Setup(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, false, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync((ActiveSessionTtl)null);

            await Assert.ThrowsAsync<SessionException>(() => logic.ValidateSessionAsync([new Claim(JwtClaimTypes.SessionId, "sid123")]));
        }

        [Fact]
        public async Task ValidateSessionAsync_WithAdditionalSessionId_ValidatesAgainstAny()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var sessionIdHash = await "sidB".HashIdStringAsync();
            var expectedId = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = sessionIdHash });

            tenantDataRepositoryMock.Setup(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, false, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync(new ActiveSessionTtl { Id = expectedId, SessionId = sessionIdHash });

            await logic.ValidateSessionAsync([], sessionIds: ["sidA", "sidB"]);

            tenantDataRepositoryMock.Verify(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, false, false, It.IsAny<TelemetryScopedLogger>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSessionAsync_DeletesStoredSession()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var sessionIdHash = await "sid123".HashIdStringAsync();
            var expectedId = await ActiveSessionTtl.IdFormatAsync(new ActiveSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = sessionIdHash });
            tenantDataRepositoryMock.Setup(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, true, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync(new ActiveSessionTtl { Id = expectedId, SessionId = sessionIdHash, TimeToLive = ActiveSessionTtl.DefaultTimeToLive });

            await logic.DeleteSessionAsync("sid123");

            tenantDataRepositoryMock.Verify(r => r.GetAsync<ActiveSessionTtl>(expectedId, false, true, false, It.IsAny<TelemetryScopedLogger>()), Times.Once);
        }

        [Fact]
        public async Task ListSessionsAsync_UsesFilter()
        {
            var (logic, tenantDataRepositoryMock, _) = CreateLogic();
            Expression<Func<ActiveSessionTtl, bool>> filter = null;

            tenantDataRepositoryMock.Setup(r => r.GetManyAsync(It.IsAny<Track.IdKey>(), It.IsAny<Expression<Func<ActiveSessionTtl, bool>>>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TelemetryScopedLogger>()))
                .Callback<Track.IdKey, Expression<Func<ActiveSessionTtl, bool>>, int, string, TelemetryScopedLogger>((idKey, f, _, __, ___) => filter = f)
                .ReturnsAsync(((IReadOnlyCollection<ActiveSessionTtl>)new List<ActiveSessionTtl>(), "token1"));

            var sessionIdHash = await "sid123".HashIdStringAsync();
            await logic.ListSessionsAsync("user@example.com", "sub1", "client1", "login", upPartyType: PartyTypes.Login, sessionId: "sid123");

            Assert.NotNull(filter);
            var matches = filter.Compile()(new ActiveSessionTtl { DataType = Constants.Models.DataType.AccessTokenSession, Email = "user@example.com", Sub = "sub1", ClientId = "client1", UpPartyName = "login", UpPartyType = PartyTypes.Login.GetPartyTypeValue(), SessionId = sessionIdHash });
            Assert.True(matches);
        }

        [Fact]
        public async Task DeleteSessionsAsync_ThrowsIfNoFilters()
        {
            var (logic, _, _) = CreateLogic();
            await Assert.ThrowsAsync<ArgumentException>(() => logic.DeleteSessionsAsync(null));
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

            await logic.DeleteSessionsAsync("user@example.com", sessionId: "sid123", clientId: "client1");

            Assert.Equal(routeBinding.TenantName, usedIdKey.TenantName);
            Assert.Equal(routeBinding.TrackName, usedIdKey.TrackName);
            var sessionIdHash = await "sid123".HashIdStringAsync();
            var matches = filter.Compile()(new ActiveSessionTtl { DataType = Constants.Models.DataType.AccessTokenSession, Email = "user@example.com", ClientId = "client1", SessionId = sessionIdHash });
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
    }
}
