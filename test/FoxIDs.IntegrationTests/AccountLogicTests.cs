using System.Threading.Tasks;
using FoxIDs.Logic;
using Xunit;
using FoxIDs.Models;
using FoxIDs.UnitTests.MockHelpers;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.Models.Logic;
using FoxIDs.Repository;
using Wololo.PgKeyValueDB;
using Npgsql;
using System.Text.Json;
using MysticMind.PostgresEmbed;
using System;

namespace FoxIDs.UnitTests
{
    public class DatabaseFixture : IDisposable
    {
        public DatabaseFixture()
        {
            PgServer = new PgServer("17.4.0", clearWorkingDirOnStart: true, clearInstanceDirOnStop: true);
            PgServer.Start();
        }

        public void Dispose()
        {
            PgServer.Stop();
        }

        public PgServer PgServer { get; private set; }
    }

    public class AccountLogicTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
    {
        [Theory]
        [InlineData("a1@test.com", "12345678")]
        [InlineData("a1@test.com", "123456789")]
        [InlineData("a1@test.com", "12345678901234567890123456789012345678901234567890")]
        public async Task CreateUserCheckPasswordLength_ReturnsUser(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordComplexity: false, checkPasswordRisk: false);
            var user = await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password);
            Assert.NotNull(user);
        }

        [Theory]
        [InlineData("a1@test.com", "1234")]
        [InlineData("a1@test.com", "12345")]
        [InlineData("a1@test.com", "123456")]
        [InlineData("a1@test.com", "1234567")]
        public async Task CreateUserCheckPasswordLength_ThrowPasswordLengthException(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordComplexity: false, checkPasswordRisk: false);
            await Assert.ThrowsAsync<PasswordLengthException>(async () => await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password));
        }

        [Theory]
        [InlineData("a1@test.com", "!QAZ2wsx")]
        [InlineData("a1@test.com", "!comQAZ2wsx")]
        [InlineData("a1@test.com", "!a1QAZ2wsx")]
        [InlineData("a1@test.com", "QAZ12wsx")]
        [InlineData("a1@test.com", "%!QAZwsx")]
        [InlineData("a1@test.com", "%!123wsx")]
        [InlineData("a1@test.com", "%!123QAZ")]
        public async Task CreateUserCheckPasswordComplexity_ReturnsUser(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordRisk: false);
            var user = await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password);
            Assert.NotNull(user);
        }

        [Theory]
        [InlineData("a1@test.com", "!testQAZ2wsx")]
        public async Task CreateUserCheckPasswordComplexity_ThrowPasswordEmailTextComplexityException(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordRisk: false);
            await Assert.ThrowsAsync<PasswordEmailTextComplexityException>(async () => await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password));
        }

        [Theory]
        [InlineData("a1@test.com", "!QAZ2wsxsometenant")]
        [InlineData("a1@test.com", "sometrack!QAZ2wsx")]
        [InlineData("a1@test.com", "!QAZ2loginwsx")]
        public async Task CreateUserCheckPasswordComplexity_ThrowPasswordUrlTextComplexityException(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordRisk: false);
            await Assert.ThrowsAsync<PasswordUrlTextComplexityException>(async () => await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password));
        }

        [Theory]
        [InlineData("a1@test.com", "!QAZ2wsx#EDC")]
        public async Task CreateUserCheckPasswordRisk_ReturnsUser(string email, string password)
        {
            var accountLogic = AccountLogicInstance();
            var user = await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password);
            Assert.NotNull(user);
        }

        [Theory]
        [InlineData("a1@test.com", "!QAZ2wsx")]
        public async Task CreateUserCheckPasswordRisk_ThrowPasswordRiskException(string email, string password)
        {
            var accountLogic = AccountLogicInstance();
            await Assert.ThrowsAsync<PasswordRiskException>(async () => await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password));
        }

        private BaseAccountLogic AccountLogicInstance(int passwordLength = 8, bool checkPasswordComplexity = true, bool checkPasswordRisk = true)
        {
            var routeBinding = new RouteBinding
            {
                PasswordLength = passwordLength,
                CheckPasswordComplexity = checkPasswordComplexity,
                CheckPasswordRisk = checkPasswordRisk,
            };
            var mockHttpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);

            var telemetryScopedLogger = TelemetryLoggerHelper.ScopedLoggerObject(mockHttpContextAccessor);

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
            var pgTenantRepository = new PgTenantDataRepository(pgTenantDb);
            var pgMasterDb = new PgKeyValueDB(dataSource, schema, "master", jsonSerializerOptions);
            pgMasterDb.Create("prisk:@master:3357229DDDC9963302283F4D4863A74F310C9E80", true, "@master:prisks");
            var pgMasterRepository = new PgMasterDataRepository(pgMasterDb);

            var secretHashLogic = new SecretHashLogic(mockHttpContextAccessor);

            var accountLogic = new BaseAccountLogic(telemetryScopedLogger, pgTenantRepository, pgMasterRepository, secretHashLogic, mockHttpContextAccessor);
            return accountLogic;
        }
    }
}
