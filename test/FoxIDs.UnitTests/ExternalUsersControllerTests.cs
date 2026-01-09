using AutoMapper;
using FoxIDs.Controllers;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using FoxIDs.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.UnitTests
{
    public class ExternalUsersControllerTests
    {
        [Fact]
        public async Task GetExternalUsers_FilterClaimValue_ReturnsExternalUser()
        {
            var controller = CreateController();

            var result = await controller.GetExternalUsers(filterValue: null, filterClaimValue: "claim-value");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<Api.PaginationResponse<Api.ExternalUser>>(okResult.Value);

            Assert.Single(response.Data);
            Assert.Contains(response.Data, user => user.UserId == "user-1");
        }

        [Fact]
        public async Task GetExternalUsers_FilterValueAndClaimValue_ReturnsUsersFromEitherFilter()
        {
            var controller = CreateController();

            var result = await controller.GetExternalUsers(filterValue: "link-two", filterClaimValue: "claim-value");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<Api.PaginationResponse<Api.ExternalUser>>(okResult.Value);

            Assert.Equal(2, response.Data.Count);
            Assert.Contains(response.Data, user => user.UserId == "user-1");
            Assert.Contains(response.Data, user => user.UserId == "user-2");
        }

        private static TExternalUsersController CreateController()
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = Mock.Of<IServiceProvider>()
            };
            httpContext.Items[Constants.Routes.RouteBindingKey] = new RouteBinding
            {
                TenantName = "testtenant",
                TrackName = "testtrack"
            };

            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var externalUsers = new List<ExternalUser>
            {
                new ExternalUser
                {
                    DataType = Constants.Models.DataType.ExternalUser,
                    UserId = "user-1",
                    UpPartyName = "up-one",
                    LinkClaimValue = "link-one",
                    RedemptionClaimValue = "redeem-one",
                    Claims = new List<ClaimAndValues>
                    {
                        new ClaimAndValues
                        {
                            Claim = "email",
                            Values = new List<string> { "claim-value" }
                        }
                    }
                },
                new ExternalUser
                {
                    DataType = Constants.Models.DataType.ExternalUser,
                    UserId = "user-2",
                    UpPartyName = "up-two",
                    LinkClaimValue = "link-two",
                    RedemptionClaimValue = "redeem-two",
                    Claims = new List<ClaimAndValues>
                    {
                        new ClaimAndValues
                        {
                            Claim = "email",
                            Values = new List<string> { "other-value" }
                        }
                    }
                }
            };

            var tenantDataRepositoryMock = new Mock<ITenantDataRepository>(MockBehavior.Strict);
            tenantDataRepositoryMock
                .Setup(r => r.GetManyAsync<ExternalUser>(
                    It.IsAny<Track.IdKey>(),
                    It.IsAny<Expression<Func<ExternalUser, bool>>>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<TelemetryScopedLogger>()))
                .Returns<Track.IdKey, Expression<Func<ExternalUser, bool>>, int, string, TelemetryScopedLogger>(
                    (idKey, whereQuery, pageSize, paginationToken, scopedLogger) =>
                    {
                        var predicate = whereQuery?.Compile() ?? (_ => true);
                        var result = externalUsers.Where(predicate).ToList();
                        return ValueTask.FromResult<(IReadOnlyCollection<ExternalUser> items, string paginationToken)>((result, null));
                    });

            var mapperMock = new Mock<IMapper>(MockBehavior.Strict);
            mapperMock
                .Setup(m => m.Map<Api.ExternalUser>(It.IsAny<ExternalUser>()))
                .Returns<ExternalUser>(user => new Api.ExternalUser
                {
                    UserId = user.UserId,
                    UpPartyName = user.UpPartyName,
                    LinkClaimValue = user.LinkClaimValue,
                    RedemptionClaimValue = user.RedemptionClaimValue
                });

            return new TExternalUsersController(
                logger,
                mapperMock.Object,
                tenantDataRepositoryMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }
    }
}
