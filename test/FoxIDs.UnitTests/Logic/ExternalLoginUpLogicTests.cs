using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using FoxIDs.UnitTests.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests.Logic
{
    public class ExternalLoginUpLogicTests
    {
        [Fact]
        public async Task LoginRedirectAsync_LogPlanUsageFalse_ReturnsRedirectWithoutPlanUsage()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant", TrackName = "track", SequenceLifetime = 300 };
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Mock.Of<IServiceProvider>()
            };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("foxids.test");
            httpContext.Items[Constants.Routes.RouteBindingKey] = routeBinding;
            httpContext.Items[Constants.Sequence.Object] = new Sequence { Id = "seq1", CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            httpContext.Items[Constants.Sequence.String] = "seqstr1";

            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var cacheProvider = new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()));
            var embeddedResourceLogic = new EmbeddedResourceLogic(httpContextAccessor);
            var localizationLogic = new LocalizationLogic(new FoxIDsSettings(), httpContextAccessor, embeddedResourceLogic);
            var sequenceLogic = new SequenceLogic(new FoxIDsSettings(), logger, DataProtectionProvider.Create("unit-test"), cacheProvider, localizationLogic, httpContextAccessor);

            var tenantDataRepositoryMock = new Mock<ITenantDataRepository>(MockBehavior.Strict);
            tenantDataRepositoryMock
                .Setup(r => r.GetAsync<ExternalLoginUpParty>(It.IsAny<string>(), true, false, false, It.IsAny<TelemetryScopedLogger>()))
                .ReturnsAsync(new ExternalLoginUpParty { Name = "external" });

            var logic = new ExternalLoginUpLogic(
                logger,
                Mock.Of<IServiceProvider>(),
                tenantDataRepositoryMock.Object,
                sequenceLogic,
                extendedUiLogic: null,
                externalUserLogic: null,
                claimTransformLogic: null,
                planUsageLogic: null,
                auditLogic: null,
                hrdLogic: null,
                httpContextAccessor);

            var partyLink = new UpPartyLink { Name = "external", Type = PartyTypes.ExternalLogin };
            var loginRequest = new LoginRequest
            {
                DownPartyLink = new DownPartySessionLink { Id = "downparty", Type = PartyTypes.Oidc }
            };

            var result = await logic.LoginRedirectAsync(partyLink, loginRequest, hrdLoginUpPartyName: "login", logPlanUsage: false);

            var redirect = Assert.IsType<RedirectResult>(result);
            var expectedUrl = httpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.ExtLoginController, includeSequence: true);
            Assert.Equal(expectedUrl, redirect.Url);
            tenantDataRepositoryMock.Verify(r => r.GetAsync<ExternalLoginUpParty>(It.IsAny<string>(), true, false, false, It.IsAny<TelemetryScopedLogger>()), Times.Once);
        }
    }
}
