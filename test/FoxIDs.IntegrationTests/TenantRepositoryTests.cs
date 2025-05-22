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

        private string GetDbId(string id) => $"user:testtenant:testtrack:{id}";

        private TenantDataRepositoryBase TenantRepositoryInstance()
        {
            (var pgTenantRepository, var pgMasterRepository) = RepositoriesHelper.GetRepositories(fixture);
            return pgTenantRepository;
        }
    }
}
