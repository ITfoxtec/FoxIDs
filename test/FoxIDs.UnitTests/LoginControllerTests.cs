using FoxIDs.Controllers;
using FoxIDs.Logic;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;
using FoxIDs.UnitTests.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class LoginControllerTests
    {
        [Fact]
        public async Task Login_PasswordlessEmailWithoutUserIdentifier_RedirectsToIdentifierStep()
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Mock.Of<IServiceProvider>()
            };

            var routeBinding = new RouteBinding
            {
                TenantName = "testtenant",
                TrackName = "testtrack",
                SequenceLifetime = 300,
                UpParty = new UpPartyWithProfile<UpPartyProfile>
                {
                    Id = "login",
                    Name = "login",
                    Type = PartyTypes.Login
                }
            };
            httpContext.Items[Constants.Routes.RouteBindingKey] = routeBinding;
            httpContext.Items[Constants.Sequence.Object] = new Sequence { Id = "seq1" };
            httpContext.Items[Constants.Sequence.String] = "seqstr1";

            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var cacheProvider = new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()));
            var embeddedResourceLogic = new EmbeddedResourceLogic(httpContextAccessor);
            var localizationLogic = new LocalizationLogic(new FoxIDsSettings(), httpContextAccessor, embeddedResourceLogic);
            var sequenceLogic = new SequenceLogic(new FoxIDsSettings(), logger, DataProtectionProvider.Create("unit-test"), cacheProvider, localizationLogic, httpContextAccessor);

            await sequenceLogic.SaveSequenceDataAsync(new LoginUpSequenceData
            {
                UpPartyId = routeBinding.UpParty.Id,
                DownPartyLink = new DownPartySessionLink { Id = "downparty", Type = PartyTypes.Oidc },
                DoLoginIdentifierStep = false,
                UserIdentifier = null
            });

            var controller = new LoginController(
                logger,
                serviceProvider: Mock.Of<IServiceProvider>(),
                localizer: Mock.Of<IStringLocalizer>(),
                tenantDataRepository: null,
                loginPageLogic: null,
                sessionLogic: null,
                sequenceLogic: sequenceLogic,
                auditLogic: null,
                securityHeaderLogic: null,
                accountLogic: null,
                dynamicElementLogic: null,
                countryCodesLogic: null,
                singleLogoutLogic: null,
                oauthRefreshTokenGrantLogic: null,
                activeSessionLogic: null)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var result = await controller.Login(passwordLessEmail: true);

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("../_seqstr1", redirect.Url);

            var updatedSequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
            Assert.True(updatedSequenceData.DoLoginIdentifierStep);
            Assert.False(updatedSequenceData.DoLoginPasswordAction);
            Assert.False(updatedSequenceData.DoLoginPasswordlessEmailAction);
            Assert.False(updatedSequenceData.DoLoginPasswordlessSmsAction);
        }
    }
}
