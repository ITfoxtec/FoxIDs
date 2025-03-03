using System.Threading.Tasks;
using FoxIDs.Logic;
using Xunit;
using FoxIDs.Models;
using FoxIDs.UnitTests.MockHelpers;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.Models.Logic;
using FoxIDs.IntegrationTests.Helpers;

namespace FoxIDs.IntegrationTests
{
    [Collection(nameof(DatabaseCollection))]
    public class AccountLogicTests(DatabaseFixture fixture)
    {
        [Theory]
        [InlineData("a1@test.com", "12345678")]
        public async Task CreateUserCheckDuplicate_ThrowUserExistsException(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordComplexity: false, checkPasswordRisk: false);
            await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password);
            await Assert.ThrowsAsync<UserExistsException>(async () => await accountLogic.CreateUserAsync(new UserIdentifier { Email = email }, password));
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

            (var pgTenantRepository, var pgMasterRepository) = RepositoriesHelper.GetRepositories(fixture);

            var secretHashLogic = new SecretHashLogic(mockHttpContextAccessor);

            var accountLogic = new BaseAccountLogic(telemetryScopedLogger, pgTenantRepository, pgMasterRepository, secretHashLogic, mockHttpContextAccessor);
            return accountLogic;
        }
    }
}
