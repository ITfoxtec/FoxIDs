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
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUsers_FilterClaimValue_ReturnsUser()
        {
            var controller = CreateController();

            var result = await controller.GetUsers(filterClaimValue: "claim-value");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<Api.PaginationResponse<Api.User>>(okResult.Value);

            Assert.Single(response.Data);
            Assert.Contains(response.Data, user => user.UserId == "user-1");
        }

        private static TUsersController CreateController()
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

            var users = new List<User>
            {
                new User
                {
                    DataType = Constants.Models.DataType.User,
                    UserId = "user-1",
                    Email = "user1@example.com",
                    Claims = new List<ClaimAndValues>
                    {
                        new ClaimAndValues
                        {
                            Claim = "role",
                            Values = new List<string> { "claim-value" }
                        }
                    }
                },
                new User
                {
                    DataType = Constants.Models.DataType.User,
                    UserId = "user-2",
                    Email = "user2@example.com",
                    Claims = new List<ClaimAndValues>
                    {
                        new ClaimAndValues
                        {
                            Claim = "role",
                            Values = new List<string> { "other-value" }
                        }
                    }
                }
            };

            var tenantDataRepositoryMock = new Mock<ITenantDataRepository>(MockBehavior.Strict);
            tenantDataRepositoryMock
                .Setup(r => r.GetManyAsync<User>(
                    It.IsAny<Track.IdKey>(),
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<TelemetryScopedLogger>()))
                .Returns<Track.IdKey, Expression<Func<User, bool>>, int, string, TelemetryScopedLogger>(
                    (idKey, whereQuery, pageSize, paginationToken, scopedLogger) =>
                    {
                        var predicate = whereQuery?.Compile() ?? (_ => true);
                        var result = users.Where(predicate).ToList();
                        return ValueTask.FromResult<(IReadOnlyCollection<User> items, string paginationToken)>((result, null));
                    });

            var mapperMock = new Mock<IMapper>(MockBehavior.Strict);
            mapperMock
                .Setup(m => m.Map<Api.User>(It.IsAny<User>()))
                .Returns<User>(user => new Api.User
                {
                    UserId = user.UserId,
                    Email = user.Email
                });

            return new TUsersController(
                logger,
                mapperMock.Object,
                tenantDataRepositoryMock.Object,
                planCacheLogic: null,
                accountLogic: null,
                tenantApiLockLogic: null)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }
    }
}
