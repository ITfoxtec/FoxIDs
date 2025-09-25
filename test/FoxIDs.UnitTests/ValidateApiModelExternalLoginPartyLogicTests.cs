using System.Collections.Generic;
using System.Threading.Tasks;
using FoxIDs.Logic;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class ValidateApiModelExternalLoginPartyLogicTests
    {
        [Fact]
        public void ValidateApiModel_WithValidCssAndIcon_SanitizesAndSetsTitle()
        {
            var (logic, modelState, party) = CreateSubject(css: "<!--html--><div>bad</div>/*safe*/.btn { color: red; }", iconUrl: "https://example.com/icon.png");

            var isValid = logic.ValidateApiModel(modelState, party);

            Assert.True(isValid);
            Assert.True(modelState.IsValid);
            Assert.Equal("/*safe*/.btn { color: red; }", party.Css);
            Assert.Equal("unitDisplay", party.Title);
        }

        [Fact]
        public void ValidateApiModel_WithUnsafeCss_AddsModelError()
        {
            var (logic, modelState, party) = CreateSubject(css: ".bad { background: expression(alert(1)); }");

            var isValid = logic.ValidateApiModel(modelState, party);

            Assert.False(isValid);
            Assert.False(modelState.IsValid);
            Assert.True(modelState.ContainsKey("css"));
            Assert.Equal(".bad { background: expression(alert(1)); }", party.Css);
        }

        [Fact]
        public void ValidateApiModel_WithUnsupportedIconExtension_AddsModelError()
        {
            var (logic, modelState, party) = CreateSubject(iconUrl: "https://example.com/icon.svg");

            var isValid = logic.ValidateApiModel(modelState, party);

            Assert.False(isValid);
            Assert.False(modelState.IsValid);
            Assert.True(modelState.ContainsKey("iconUrl"));
        }

        private static (ValidateApiModelExternalLoginPartyLogic logic, ModelStateDictionary modelState, Api.ExternalLoginUpParty party) CreateSubject(string css = null, string iconUrl = null)
        {
            var routeBinding = new RouteBinding { TenantName = "tenant", TrackName = "track", DisplayName = "unitDisplay" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var telemetryLogger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var dynamicElementLogic = new ValidateApiModelDynamicElementLogic(telemetryLogger, httpContextAccessor);
            var genericPartyLogic = new ValidateApiModelGenericPartyLogic(telemetryLogger, dynamicElementLogic, httpContextAccessor);
            var logic = new ValidateApiModelExternalLoginPartyLogic(telemetryLogger, genericPartyLogic, dynamicElementLogic, httpContextAccessor);

            var party = new Api.ExternalLoginUpParty
            {
                Name = "ext",
                ExternalLoginType = Api.ExternalConnectTypes.Api,
                UsernameType = Api.ExternalLoginUsernameTypes.Email,
                Css = css,
                IconUrl = iconUrl,
                Title = null,
                ExitClaimTransforms = new List<Api.OAuthClaimTransform>(),
                ExtendedUis = new List<Api.ExtendedUi>(),
                LinkExternalUser = new Api.LinkExternalUser { Elements = new List<Api.DynamicElement>(), ClaimTransforms = new List<Api.OAuthClaimTransform>() },
                HrdIPAddressesAndRanges = new List<string>(),
                HrdRegularExpressions = new List<string>()
            };

            var modelState = new ModelStateDictionary();
            return (logic, modelState, party);
        }
    }
}
