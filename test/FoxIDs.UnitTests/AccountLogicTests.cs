using System.Threading.Tasks;
using FoxIDs.Logic;
using Xunit;
using FoxIDs.Models;
using FoxIDs.UnitTests.MockHelpers;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.Mocks;

namespace FoxIDs.UnitTests
{
    public class AccountLogicTests
    {
        [Theory]
        [InlineData("a1@test.com", "12345678")]
        [InlineData("a1@test.com", "123456789")]
        [InlineData("a1@test.com", "12345678901234567890123456789012345678901234567890")]
        public async Task CreateUserCheckPasswordLength_ReturnsUser(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordComplexity: false, checkPasswordRisk: false);
            var user = await accountLogic.CreateUser(email, password);
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
            await Assert.ThrowsAsync<PasswordLengthException>(async () => await accountLogic.CreateUser(email, password));
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
            var user = await accountLogic.CreateUser(email, password);
            Assert.NotNull(user);
        }

        [Theory]
        [InlineData("a1@test.com", "!testQAZ2wsx")]
        public async Task CreateUserCheckPasswordComplexity_ThrowPasswordEmailTextComplexityException(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordRisk: false);
            await Assert.ThrowsAsync<PasswordEmailTextComplexityException>(async () => await accountLogic.CreateUser(email, password));
        }

        [Theory]
        [InlineData("a1@test.com", "!QAZ2wsxsometenant")]
        [InlineData("a1@test.com", "sometrack!QAZ2wsx")]
        [InlineData("a1@test.com", "!QAZ2loginwsx")]
        public async Task CreateUserCheckPasswordComplexity_ThrowPasswordUrlTextComplexityException(string email, string password)
        {
            var accountLogic = AccountLogicInstance(checkPasswordRisk: false);
            await Assert.ThrowsAsync<PasswordUrlTextComplexityException>(async () => await accountLogic.CreateUser(email, password));
        }

        [Theory]
        [InlineData("a1@test.com", "!QAZ2wsx#EDC")]
        public async Task CreateUserCheckPasswordRisk_ReturnsUser(string email, string password)
        {
            var accountLogic = AccountLogicInstance();
            var user = await accountLogic.CreateUser(email, password);
            Assert.NotNull(user);
        }

        [Theory]
        [InlineData("a1@test.com", "!QAZ2wsx")]
        public async Task CreateUserCheckPasswordRisk_ThrowPasswordRiskException(string email, string password)
        {
            var accountLogic = AccountLogicInstance();
            await Assert.ThrowsAsync<PasswordRiskException>(async () => await accountLogic.CreateUser(email, password));
        }

        private AccountLogic AccountLogicInstance(int passwordLength = 8, bool checkPasswordComplexity = true, bool checkPasswordRisk = true)
        {            
            var routeBinding = new RouteBinding
            {
                PasswordLength = passwordLength,
                CheckPasswordComplexity = checkPasswordComplexity,
                CheckPasswordRisk = checkPasswordRisk,
            };
            var mockHttpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);

            var telemetryScopedLogger = TelemetryLoggerHelper.ScopedLoggerObject(mockHttpContextAccessor);

            var fakeTenantRepository = new FakeTenantRepository();
            var fakeMasterRepository = new FakeMasterRepository();
           
            var secretHashLogic = new SecretHashLogic(mockHttpContextAccessor);

            var accountLogic = new AccountLogic(telemetryScopedLogger, fakeTenantRepository, fakeMasterRepository, secretHashLogic, mockHttpContextAccessor);
            return accountLogic;
        }
    }
}
