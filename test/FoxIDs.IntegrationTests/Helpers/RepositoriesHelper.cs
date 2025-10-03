using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Npgsql;
using System;
using System.Text.Json;
using Wololo.PgKeyValueDB;

namespace FoxIDs.IntegrationTests.Helpers
{
    public static class RepositoriesHelper
    {
        public static (PgTenantDataRepository pgTenantRepository, PgMasterDataRepository pgMasterRepository) GetRepositories(DatabaseFixture fixture)
        {
            var connectionString = $"Host=localhost;Port={fixture.PgServer.PgPort};Username=postgres;Password=postgres;Database=postgres";
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var dataSource = new NpgsqlDataSourceBuilder(connectionString)
                .EnableDynamicJson()
                .ConfigureJsonOptions(jsonSerializerOptions)
                .Build();
            var schema = "foxids" + Guid.NewGuid().ToString().Replace("-", "");

            var pgTenantDb = new PgKeyValueDB(dataSource, schema, "tenant", jsonSerializerOptions);
            SeedTenantDb(pgTenantDb);
            var httpContextAccessor = new HttpContextAccessor();
            var pgTenantRepository = new PgTenantDataRepository(pgTenantDb, httpContextAccessor);

            var pgMasterDb = new PgKeyValueDB(dataSource, schema, "master", jsonSerializerOptions);
            SeedMasterDb(pgMasterDb);
            var pgMasterRepository = new PgMasterDataRepository(pgMasterDb);

            return (pgTenantRepository, pgMasterRepository);
        }

        private static void SeedTenantDb(PgKeyValueDB pgTenantDb)
        {
            var mikeParkerUser = new User
            {
                Id = "user:testtenant:testtrack:mike.parker@test.org",
                PartitionId = "testtenant:testtrack",
                AdditionalIds = ["user:testtenant:testtrack:mike.parker@test.org", "user:testtenant:testtrack:+4522222222", "user:testtenant:testtrack:mike.parker"],
                UserId = "8345e574-7460-49f4-957f-417ead77f852",
                DataType = "user",
                Email = "mike.parker@test.org",
                Phone = "+4522222222",
                Username = "mike.parker",
                Hash = "tsW9vAV4Duk0Q6FnJWVsncN30_ccUG0LJczhXRKjGDgIVd0Y_T41Dx4PpA-rltOlhD-dJMIulICo32hL7UdrlAcUx6XHwC9NmsblxaISCUs",
                HashAlgorithm = "P2HS512:10",
                HashSalt = "a3B0ezJV9I2mWpOQAWKvDnn5dXQVxFOZjZPrU1FTl6kj0fSOCyqPJFqNjnDAp7YsI_qzKQvRTvYQzD_wtplMXA",
            };
            pgTenantDb.Create(mikeParkerUser.Id, mikeParkerUser, mikeParkerUser.PartitionId);

            var mariaMutchUser = new User
            {
                Id = "user:testtenant:testtrack:maria.mutch@test.org",
                PartitionId = "testtenant:testtrack",
                AdditionalIds = ["user:testtenant:testtrack:maria.mutch@test.org", "user:testtenant:testtrack:+4566666666", "user:testtenant:testtrack:maria.mutch"],
                UserId = "07e5a739-ab63-4c41-86df-655c562a55ba",
                DataType = "user",
                Email = "maria.mutch@test.org",
                Phone = "+4566666666",
                Username = "maria.mutch",
                Hash = "tsW9vAV4Duk0Q6FnJWVsncN30_ccUG0LJczhXRKjGDgIVd0Y_T41Dx4PpA-rltOlhD-dJMIulICo32hL7UdrlAcUx6XHwC9NmsblxaISCUs",
                HashAlgorithm = "P2HS512:10",
                HashSalt = "a3B0ezJV9I2mWpOQAWKvDnn5dXQVxFOZjZPrU1FTl6kj0fSOCyqPJFqNjnDAp7YsI_qzKQvRTvYQzD_wtplMXA",
            };
            pgTenantDb.Create(mariaMutchUser.Id, mariaMutchUser, mariaMutchUser.PartitionId);
        }

        private static void SeedMasterDb(PgKeyValueDB pgMasterDb)
        {
            pgMasterDb.Create("prisk:@master:3357229DDDC9963302283F4D4863A74F310C9E80", true, "@master:prisks");
        }
    }
}
