using FoxIDs.Models;
using Google.Authenticator;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;

namespace FoxIDs.Logic
{
    public class AccountTwoFactorAppLogic : LogicSequenceBase
    {
        private static TimeSpan timeTolerance = TimeSpan.FromMinutes(2);
        private const int secretAndRecoveryCodeLength = 30;
        protected readonly TelemetryScopedLogger logger;
        protected readonly ITenantDataRepository tenantDataRepository;
        protected readonly SecretHashLogic secretHashLogic;
        private readonly AccountLogic accountLogic;
        private readonly FailingLoginLogic failingLoginLogic;

        public AccountTwoFactorAppLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, SecretHashLogic secretHashLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.secretHashLogic = secretHashLogic;
            this.accountLogic = accountLogic;
            this.failingLoginLogic = failingLoginLogic;
        }

        public async Task<TwoFactorSetupInfo> GenerateSetupCodeAsync(string twoFactorAppName, string email)
        {
            email = email?.ToLower();
            var twoFactor = new TwoFactorAuthenticator();
            var secret = RandomGenerator.Generate(secretAndRecoveryCodeLength);
            var setupInfo = await Task.FromResult(twoFactor.GenerateSetupCode(twoFactorAppName, email, secret, false, 3));

            return new TwoFactorSetupInfo
            {
                Secret = secret,
                QrCodeSetupImageUrl = setupInfo.QrCodeSetupImageUrl,
                ManualSetupKey = setupInfo.ManualEntryKey
            };
        } 
        
        public async Task ValidateTwoFactorBySecretAsync(string userIdentifier, string secret, string appCode)
        {
            userIdentifier = userIdentifier?.ToLower();
            var failingTwoFactorCount = await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, FailingLoginTypes.TwoFactorAuthenticator);

            var twoFactor = new TwoFactorAuthenticator();
            bool isValid = await Task.FromResult(twoFactor.ValidateTwoFactorPIN(secret, appCode, timeTolerance, false));

            if (isValid)
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(userIdentifier, FailingLoginTypes.TwoFactorAuthenticator);
                logger.ScopeTrace(() => $"User '{userIdentifier}' two-factor app code is valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingTwoFactorCount), triggerEvent: true);
            }
            else 
            {
                var increasedTwoFactorCount = await failingLoginLogic.IncreaseFailingLoginOrSendingCountAsync(userIdentifier, FailingLoginTypes.TwoFactorAuthenticator);
                logger.ScopeTrace(() => $"Failing two-factor count increased for user '{userIdentifier}', two-factor app code invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedTwoFactorCount), triggerEvent: true);
                throw new InvalidAppCodeException($"Invalid two-factor app code, user '{userIdentifier}'.");
            }
        }

        public string CreateRecoveryCode()
        {
            return Base32Encoding.ToString(RandomGenerator.GenerateBytes(secretAndRecoveryCodeLength)).TrimEnd('=');
        }

        public async Task<User> SetTwoFactorAppSecretUser(string userIdentifier, string newSecret, string twoFactorAppRecoveryCode)
        {
            userIdentifier = userIdentifier?.ToLower();
            logger.ScopeTrace(() => $"Set two-factor app secret user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            var user = await accountLogic.GetUserAsync(userIdentifier);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled, trying to set two-factor app secret.");
            }          

            user.TwoFactorAppSecret = newSecret;

            var recoveryCode = new TwoFactorAppRecoveryCode();
            await secretHashLogic.AddSecretHashAsync(recoveryCode, twoFactorAppRecoveryCode);
            user.TwoFactorAppRecoveryCode = recoveryCode;
            await tenantDataRepository.SaveAsync(user);

            logger.ScopeTrace(() => $"User '{userIdentifier}', two-factor app secret set.", triggerEvent: true);
            return user;
        }

        public async Task<User> ValidateTwoFactorAppRecoveryCodeUser(string userIdentifier, string twoFactorAppRecoveryCode)
        {
            userIdentifier = userIdentifier?.ToLower();
            logger.ScopeTrace(() => $"Validating two-factor app recovery code user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            var failingTwoFactorCount = await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, FailingLoginTypes.TwoFactorAuthenticator);

            var user = await accountLogic.GetUserAsync(userIdentifier);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled, trying to validate two-factor app recovery code.");
            }

            if (user.TwoFactorAppRecoveryCode == null)
            {
                throw new InvalidOperationException($"User '{userIdentifier}' do not have a two-factor app recovery code, trying to validate two-factor app recovery code.");
            }

            if (await secretHashLogic.ValidateSecretAsync(user.TwoFactorAppRecoveryCode, twoFactorAppRecoveryCode))
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(userIdentifier, FailingLoginTypes.TwoFactorAuthenticator);
                logger.ScopeTrace(() => $"User '{userIdentifier}' two-factor app recovery code is valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingTwoFactorCount), triggerEvent: true);
                return user;
            }
            else
            {
                var increasedTwoFactorCount = await failingLoginLogic.IncreaseFailingLoginOrSendingCountAsync(userIdentifier, FailingLoginTypes.TwoFactorAuthenticator);
                logger.ScopeTrace(() => $"Failing two-factor count increased for user '{userIdentifier}', two-factor app recovery code invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedTwoFactorCount), triggerEvent: true);
                throw new InvalidRecoveryCodeException($"Two-factor app recovery code invalid, user '{userIdentifier}'.");
            }
        }
    }
}
