using System.Threading.Tasks;
using Xunit;
using FoxIDs.Models;
using FoxIDs.Repository;
using FoxIDs.IntegrationTests.Helpers;
using System.Linq.Expressions;
using System;
using Xunit.Abstractions;
using ITfoxtec.Identity;
using System.Linq;
using FoxIDs.Logic;

namespace FoxIDs.IntegrationTests
{
    [Collection(nameof(DatabaseCollection))]
    public class TenantRepositoryTests(DatabaseFixture fixture, ITestOutputHelper output) 
    {
        [Theory]
        [InlineData("mike.parker@test.org")]
        [InlineData("+4522222222")]
        [InlineData("mike.parker")]
        [InlineData("maria.mutch@test.org")]
        [InlineData("+4566666666")]
        [InlineData("maria.mutch")]
        public async Task GetListUsersByAdditionalId_ReturnsUsers(string additionalId)
        {
            var tenantRepository = TenantRepositoryInstance();
            var dbId = GetDbId(additionalId);
            Expression<Func<User, bool>> whereQuery = q => q.AdditionalIds.Contains(dbId);
            (var items, string paginationToken) = await tenantRepository.GetManyAsync(new Track.IdKey { TenantName = "testtenant", TrackName = "testtrack" }, whereQuery, 1);

            output.WriteLine($"Test additionalId={additionalId}");
            foreach (var item in items)
            {
                output.WriteLine(item.ToJsonIndented());
            }

            Assert.True(items.Count() == 1);
        }

        [Theory]
        [InlineData("mike.parker@test.org")]
        [InlineData("maria.mutch@test.org")]
        public async Task GetListUsersById_ReturnsUsers(string id)
        {
            var tenantRepository = TenantRepositoryInstance();
            var dbId = GetDbId(id);
            Expression<Func<User, bool>> whereQuery = q => q.Id == dbId; 
            (var items, string paginationToken) = await tenantRepository.GetManyAsync(new Track.IdKey { TenantName = "testtenant", TrackName = "testtrack" }, whereQuery, 1);

            output.WriteLine($"Test id={id}");
            foreach (var item in items)
            {
                output.WriteLine(item.ToJsonIndented());
            }

            Assert.True(items.Count() == 1);
        }

        [Theory]
        [InlineData("mike.parker@test.org")]
        [InlineData("+4522222222")]
        [InlineData("mike.parker")]
        [InlineData("maria.mutch@test.org")]
        [InlineData("+4566666666")]
        [InlineData("maria.mutch")]
        public async Task GetListUsersByIdOrAdditionalId_ReturnsUsers(string idOrAdditionalId)
        {
            var tenantRepository = TenantRepositoryInstance();
            var dbId = GetDbId(idOrAdditionalId);
            Expression<Func<User, bool>> whereQuery = q => q.Id == dbId || q.AdditionalIds.Contains(dbId);
            (var items, string paginationToken) = await tenantRepository.GetManyAsync(new Track.IdKey { TenantName = "testtenant", TrackName = "testtrack" }, whereQuery, 1);

            output.WriteLine($"Test idOrAdditionalId={idOrAdditionalId}");
            foreach (var item in items)
            {
                output.WriteLine(item.ToJsonIndented());
            }

            Assert.True(items.Count() == 1);
        }

        [Theory]
        [InlineData("mike.parker@test.org")]
        [InlineData("+4522222222")]
        [InlineData("mike.parker")]
        [InlineData("maria.mutch@test.org")]
        [InlineData("+4566666666")]
        [InlineData("maria.mutch")]
        public async Task GetUserByIdOrAdditionalId_ReturnsUser(string idOrAdditionalId)
        {
            var tenantRepository = TenantRepositoryInstance();
            var item = await tenantRepository.GetAsync<User>(GetDbId(idOrAdditionalId), queryAdditionalIds: true);

            output.WriteLine($"Test idOrAdditionalId={idOrAdditionalId}");
            output.WriteLine(item.ToJsonIndented());

            Assert.NotNull(item);
        }


        [Theory]
        [InlineData("mike.parker@test.org")]
        [InlineData("maria.mutch@test.org")]
        public async Task GetUserById_ReturnsUser(string id)
        {
            var tenantRepository = TenantRepositoryInstance();
            var item = await tenantRepository.GetAsync<User>(GetDbId(id));

            output.WriteLine($"Test id={id}");
            output.WriteLine(item.ToJsonIndented());

            Assert.NotNull(item);
        }

        [Fact]
        public async Task GetListAllUsers_ReturnsUsers()
        {
            var tenantRepository = TenantRepositoryInstance();
            (var items, string paginationToken) = await tenantRepository.GetManyAsync<User>(new Track.IdKey { TenantName = "testtenant", TrackName = "testtrack" });

            foreach (var item in items)
            {
                output.WriteLine(item.ToJsonIndented());
            }

            Assert.True(items.Count >= 1);
        }

        [Theory]
        [InlineData("mike.parker", null, null, null, 1)] // Filter by email
        [InlineData(null, "+4522222222", null, null, 1)] // Filter by phone
        [InlineData(null, null, "mike.parker", null, 1)] // Filter by username
        [InlineData(null, null, null, "8345e574-7460-49f4-957f-417ead77f852", 1)] // Filter by userId
        [InlineData("maria", null, null, null, 1)] // Partial email match
        [InlineData(null, "+456", null, null, 1)] // Partial phone match
        [InlineData(null, null, "maria", null, 1)] // Partial username match
        [InlineData(null, null, null, "07e5a739", 1)] // Partial userId match
        [InlineData("nonexistent", null, null, null, 0)] // No matches
        [InlineData("mike", "+456", null, null, 2)] // Multiple filters (OR condition)
        public async Task GetUsers_WithFilters_ReturnsFilteredUsers(string filterEmail, string filterPhone, string filterUsername, string filterUserId, int expectedCount)
        {
            var tenantRepository = TenantRepositoryInstance();
            var idKey = new Track.IdKey { TenantName = "testtenant", TrackName = "testtrack" };

            // Use the same expression logic as the controller
            var whereQuery = UserFilterLogic.CreateUserFilterExpression(filterEmail, filterPhone, filterUsername, filterUserId);

            (var items, string paginationToken) = await tenantRepository.GetManyAsync<User>(idKey, whereQuery: whereQuery);

            output.WriteLine($"Test filters - Email: {filterEmail}, Phone: {filterPhone}, Username: {filterUsername}, UserId: {filterUserId}");
            output.WriteLine($"Expected: {expectedCount}, Actual: {items.Count()}");
            
            foreach (var item in items)
            {
                output.WriteLine($"Found user: Email={item.Email}, Phone={item.Phone}, Username={item.Username}, UserId={item.UserId}");
            }

            Assert.Equal(expectedCount, items.Count());
        }

        [Fact]
        public async Task GetUsers_NoFilters_ReturnsAllUsers()
        {
            var tenantRepository = TenantRepositoryInstance();
            var idKey = new Track.IdKey { TenantName = "testtenant", TrackName = "testtrack" };

            // Test the no-filter case (all filters are null/empty)
            var whereQuery = UserFilterLogic.CreateUserFilterExpression();

            (var items, string paginationToken) = await tenantRepository.GetManyAsync<User>(idKey, whereQuery: whereQuery);

            output.WriteLine($"Found {items.Count()} users without filters");
            foreach (var item in items)
            {
                output.WriteLine($"User: Email={item.Email}, Phone={item.Phone}, Username={item.Username}, UserId={item.UserId}");
            }

            Assert.True(items.Count() >= 2); // Should return both seeded users
        }

        [Theory]
        [InlineData("MIKE.PARKER", 1)] // Case insensitive email search
        [InlineData("MARIA.MUTCH", 1)] // Case insensitive username search
        public async Task GetUsers_CaseInsensitiveFilters_ReturnsUsers(string filterValue, int expectedCount)
        {
            var tenantRepository = TenantRepositoryInstance();
            var idKey = new Track.IdKey { TenantName = "testtenant", TrackName = "testtrack" };

            // Test case insensitive filtering on email and username
            var whereQuery = UserFilterLogic.CreateUserFilterExpression(filterEmail: filterValue, filterUsername: filterValue);

            (var items, string paginationToken) = await tenantRepository.GetManyAsync<User>(idKey, whereQuery: whereQuery);

            output.WriteLine($"Case insensitive test with filter: {filterValue}");
            output.WriteLine($"Expected: {expectedCount}, Actual: {items.Count()}");
            
            foreach (var item in items)
            {
                output.WriteLine($"Found user: Email={item.Email}, Username={item.Username}");
            }

            Assert.Equal(expectedCount, items.Count());
        }

        private string GetDbId(string id) => $"user:testtenant:testtrack:{id}";

        private TenantDataRepositoryBase TenantRepositoryInstance()
        {
            (var pgTenantRepository, var pgMasterRepository) = RepositoriesHelper.GetRepositories(fixture);
            return pgTenantRepository;
        }
    }
}
