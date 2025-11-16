using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Repository;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using ITfoxtec.Identity;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests.Logic
{
    public class AccessTokenSessionLogicTests
    {
        [Fact]
        public async Task SaveSessionAsync_WithSessionId_SavesSession()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            AccessTokenSessionTtl savedSession = null;

            tenantDataRepositoryMock.Setup(r => r.SaveAsync(It.IsAny<AccessTokenSessionTtl>(), It.IsAny<TelemetryScopedLogger>()))
                .Callback<AccessTokenSessionTtl, TelemetryScopedLogger>((session, _) => savedSession = session)
                .Returns(ValueTask.CompletedTask);

            await logic.SaveSessionAsync([new Claim(JwtClaimTypes.SessionId, "sid123")]);

            tenantDataRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<AccessTokenSessionTtl>(), It.IsAny<TelemetryScopedLogger>()), Times.Once);
            Assert.NotNull(savedSession);
            var expectedId = await AccessTokenSessionTtl.IdFormatAsync(new AccessTokenSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = "sid123" });
            Assert.Equal(expectedId, savedSession.Id);
            Assert.Equal("sid123", savedSession.SessionId);
            Assert.Equal(AccessTokenSessionTtl.DefaultTimeToLive, savedSession.TimeToLive);
        }

        [Fact]
        public async Task ValidateSessionAsync_WhenSessionMissing_ThrowsSessionException()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var expectedId = await AccessTokenSessionTtl.IdFormatAsync(new AccessTokenSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = "sid123" });

            tenantDataRepositoryMock.Setup(r => r.GetAsync<AccessTokenSessionTtl>(expectedId, false, false, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync((AccessTokenSessionTtl)null);

            await Assert.ThrowsAsync<SessionException>(() => logic.ValidateSessionAsync([new Claim(JwtClaimTypes.SessionId, "sid123")]));
        }

        [Fact]
        public async Task DeleteSessionAsync_DeletesStoredSession()
        {
            var (logic, tenantDataRepositoryMock, routeBinding) = CreateLogic();
            var expectedId = await AccessTokenSessionTtl.IdFormatAsync(new AccessTokenSessionTtl.IdKey { TenantName = routeBinding.TenantName, TrackName = routeBinding.TrackName, SessionIdHash = "sid123" });
            tenantDataRepositoryMock.Setup(r => r.GetAsync<AccessTokenSessionTtl>(expectedId, false, true, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync(new AccessTokenSessionTtl { Id = expectedId, SessionId = "sid123", TimeToLive = AccessTokenSessionTtl.DefaultTimeToLive });

            await logic.DeleteSessionAsync("sid123");

            tenantDataRepositoryMock.Verify(r => r.GetAsync<AccessTokenSessionTtl>(expectedId, false, true, false, It.IsAny<TelemetryScopedLogger>()), Times.Once);
        }

        private static (OAuthAccessTokenSessionLogic logic, Mock<ITenantDataRepository> repositoryMock, RouteBinding routeBinding) CreateLogic()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant1", TrackName = "track1" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var telemetryScopedLogger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);
            var repositoryMock = new Mock<ITenantDataRepository>();

            var logic = new OAuthAccessTokenSessionLogic(telemetryScopedLogger, repositoryMock.Object, httpContextAccessor);
            return (logic, repositoryMock, routeBinding);
        }
    }
}
