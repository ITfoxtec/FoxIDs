using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using ITfoxtec.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests.Logic
{
    public class ExternalUserLogicTests
    {
        [Fact]
        public async Task HandleUserAsync_ExpiredExternalUser_DeletesAndReturnsNoClaims()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant1", TrackName = "track1" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var expiredExternalUser = new ExternalUser
            {
                Id = "extu:tenant1:track1:upparty:hash",
                ExpireAt = DateTimeOffset.UtcNow.AddSeconds(-10).ToUnixTimeSeconds()
            };

            var tenantDataRepositoryMock = new Mock<ITenantDataRepository>(MockBehavior.Strict);
            tenantDataRepositoryMock
                .Setup(r => r.GetAsync<ExternalUser>(It.IsAny<string>(), false, false, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync(expiredExternalUser);
            tenantDataRepositoryMock
                .Setup(r => r.DeleteAsync<ExternalUser>(expiredExternalUser.Id, false, It.IsAny<TelemetryScopedLogger>()))
                .Returns(ValueTask.CompletedTask);

            var logic = new ExternalUserLogic(
                logger,
                tenantDataRepositoryMock.Object,
                sequenceLogic: null,
                claimsDownLogic: null,
                claimTransformLogic: null,
                httpContextAccessor);

            var upParty = new OidcUpParty
            {
                Name = "upparty",
                LinkExternalUser = new LinkExternalUser
                {
                    LinkClaimType = JwtClaimTypes.Subject,
                    AutoCreateUser = false,
                    RequireUser = false
                }
            };

            var loginRequest = new LoginRequest
            {
                DownPartyLink = new DownPartySessionLink { Id = "downparty", Type = PartyTypes.Oidc }
            };
            var claims = new List<Claim> { new Claim(JwtClaimTypes.Subject, "link-value") };

            var (externalUserClaims, actionResult, deleteSequenceData) = await logic.HandleUserAsync(
                upParty,
                loginRequest,
                claims,
                _ => { },
                _ => { });

            Assert.Null(externalUserClaims);
            Assert.Null(actionResult);
            Assert.False(deleteSequenceData);
            tenantDataRepositoryMock.Verify(r => r.DeleteAsync<ExternalUser>(expiredExternalUser.Id, false, It.IsAny<TelemetryScopedLogger>()), Times.Once);
        }

        [Fact]
        public async Task HandleUserAsync_ExtendsExternalUserExpireAtOnLogin()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant1", TrackName = "track1" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var initialExpireAt = DateTimeOffset.UtcNow.AddSeconds(5).ToUnixTimeSeconds();
            var externalUser = new ExternalUser
            {
                Id = "extu:tenant1:track1:upparty:hash",
                UserId = "user-1",
                ExpireAt = initialExpireAt,
                ExpireInSeconds = 60
            };

            var tenantDataRepositoryMock = new Mock<ITenantDataRepository>(MockBehavior.Strict);
            tenantDataRepositoryMock
                .Setup(r => r.GetAsync<ExternalUser>(It.IsAny<string>(), false, false, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync(externalUser);
            tenantDataRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<ExternalUser>(), It.IsAny<TelemetryScopedLogger>()))
                .Returns(ValueTask.CompletedTask);

            var logic = new ExternalUserLogic(
                logger,
                tenantDataRepositoryMock.Object,
                sequenceLogic: null,
                claimsDownLogic: null,
                claimTransformLogic: null,
                httpContextAccessor);

            var upParty = new OidcUpParty
            {
                Name = "upparty",
                LinkExternalUser = new LinkExternalUser
                {
                    LinkClaimType = JwtClaimTypes.Subject,
                    AutoCreateUser = false,
                    RequireUser = false
                }
            };

            var loginRequest = new LoginRequest
            {
                DownPartyLink = new DownPartySessionLink { Id = "downparty", Type = PartyTypes.Oidc }
            };
            var claims = new List<Claim> { new Claim(JwtClaimTypes.Subject, "link-value") };

            await logic.HandleUserAsync(
                upParty,
                loginRequest,
                claims,
                _ => { },
                _ => { });

            Assert.True(externalUser.ExpireAt > initialExpireAt);
            tenantDataRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ExternalUser>(), It.IsAny<TelemetryScopedLogger>()), Times.Once);
        }
    }
}
