using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Logic;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class ValidateApiModelLoginPartyLogicTests
    {
        [Fact]
        public async Task ValidateApiModelAsync_WithValidCssAndIcon_SanitizesAndPasses()
        {
            var (logic, modelState, routeBinding) = CreateSubject();
            var party = new Api.LoginUpParty
            {
                Css = "/* comment */ .btn { color: red; }",
                IconUrl = "https://example.com/icon.png",
                Elements = new List<Api.DynamicElement>(),
                EnableCreateUser = false
            };

            var isValid = await logic.ValidateApiModelAsync(modelState, party);

            Assert.True(isValid);
            Assert.True(modelState.IsValid);
            Assert.Equal(".btn { color: red; }", party.Css);
            Assert.Equal(routeBinding.TenantName, party.TwoFactorAppName);
            Assert.Null(party.Elements);
        }

        [Fact]
        public async Task ValidateApiModelAsync_WithUnsafeCss_ReturnsFalseAndAddsModelError()
        {
            var (logic, modelState, _) = CreateSubject();
            var css = ".danger { background-image: url(\"javascript:alert(1)\"); }";
            var party = new Api.LoginUpParty
            {
                Css = css,
                IconUrl = "https://example.com/icon.png",
                EnableCreateUser = false
            };

            var isValid = await logic.ValidateApiModelAsync(modelState, party);

            Assert.False(isValid);
            Assert.False(modelState.IsValid);
            Assert.True(modelState.ContainsKey("css"));
            Assert.Equal(css, party.Css);
        }

        [Fact]
        public async Task ValidateApiModelAsync_WithUnsupportedIconExtension_AddsModelError()
        {
            var (logic, modelState, _) = CreateSubject();
            var party = new Api.LoginUpParty
            {
                IconUrl = "https://example.com/icon.svg",
                EnableCreateUser = false
            };

            var isValid = await logic.ValidateApiModelAsync(modelState, party);

            Assert.False(isValid);
            Assert.False(modelState.IsValid);
            Assert.True(modelState.ContainsKey("iconUrl"));
        }

        private static (ValidateApiModelLoginPartyLogic logic, ModelStateDictionary modelState, RouteBinding routeBinding) CreateSubject()
        {
            var routeBinding = new RouteBinding { TenantName = "unitTenant", TrackName = "unitTrack" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var telemetryLogger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var dynamicElementLogic = new ValidateApiModelDynamicElementLogic(telemetryLogger, httpContextAccessor);
            var genericPartyLogic = new ValidateApiModelGenericPartyLogic(telemetryLogger, dynamicElementLogic, httpContextAccessor);

            var settings = new Settings { Cache = new CacheSettings { PlanLifetime = 60 } };
            var cacheProviderMock = new Mock<IDataCacheProvider>();
            cacheProviderMock.Setup(p => p.DeleteAsync(It.IsAny<string>())).Returns(ValueTask.CompletedTask);
            cacheProviderMock.Setup(p => p.GetAsync(It.IsAny<string>())).Returns(new ValueTask<string>((string)null));
            cacheProviderMock.Setup(p => p.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(ValueTask.CompletedTask);

            var masterRepositoryMock = new Mock<IMasterDataRepository>();

            var planCacheLogic = new PlanCacheLogic(settings, cacheProviderMock.Object, masterRepositoryMock.Object, httpContextAccessor);

            var logic = new ValidateApiModelLoginPartyLogic(telemetryLogger, planCacheLogic, genericPartyLogic, dynamicElementLogic, httpContextAccessor);
            var modelState = new ModelStateDictionary();

            return (logic, modelState, routeBinding);
        }
    }
}
