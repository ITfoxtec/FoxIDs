using FoxIDs.Models;
using Google.Authenticator;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models.Logic;

namespace FoxIDs.Logic
{
    public class AccountTwoFactorLogic : LogicSequenceBase
    {
        private static TimeSpan timeTolerance = TimeSpan.FromMinutes(2);
        private const int secretAndRecoveryCodeLength = 30;
        protected readonly TelemetryScopedLogger logger;
        protected readonly ITenantDataRepository tenantDataRepository;
        protected readonly SecretHashLogic secretHashLogic;
        private readonly AccountLogic accountLogic;
        private readonly FailingLoginLogic failingLoginLogic;

        public AccountTwoFactorLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, SecretHashLogic secretHashLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.secretHashLogic = secretHashLogic;
            this.accountLogic = accountLogic;
            this.failingLoginLogic = failingLoginLogic;
        }

        public async Task<TwoFactorSetupInfo> GenerateSetupCodeAsync(string twoFactorAppName, string email)
        {
            email = email?.ToLowerInvariant();
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
        
        public async Task ValidateTwoFactorBySecretAsync(string email, string secret, string appCode)
        {
            email = email?.ToLowerInvariant();
            var failingTwoFactorCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email, FailingLoginTypes.TwoFactorAuthenticator);

            var twoFactor = new TwoFactorAuthenticator();
            bool isValid = await Task.FromResult(twoFactor.ValidateTwoFactorPIN(secret, appCode, timeTolerance, false));

            if (isValid)
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(email, FailingLoginTypes.TwoFactorAuthenticator);
                logger.ScopeTrace(() => $"User '{email}' two-factor app code is valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingTwoFactorCount), triggerEvent: true);
            }
            else 
            {
                var increasedTwoFactorCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email, FailingLoginTypes.TwoFactorAuthenticator);
                logger.ScopeTrace(() => $"Failing two-factor count increased for user '{email}', two-factor app code invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedTwoFactorCount), triggerEvent: true);
                throw new InvalidAppCodeException($"Invalid two-factor app code, user '{email}'.");
            }
        }

        public string CreateRecoveryCode()
        {
            return Base32Encoding.ToString(RandomGenerator.GenerateBytes(secretAndRecoveryCodeLength)).TrimEnd('=');
        }

        public async Task<User> SetTwoFactorAppSecretUser(string email, string newSecret, string twoFactorAppRecoveryCode)
        {
            email = email?.ToLowerInvariant();
            logger.ScopeTrace(() => $"Set two-factor app secret user '{email}', Route '{RouteBinding?.Route}'.");

            var user = await accountLogic.GetUserAsync(email);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{user.Email}' do not exist or is disabled, trying to set two-factor app secret.");
            }          

            user.TwoFactorAppSecret = newSecret;

            var recoveryCode = new TwoFactorAppRecoveryCode();
            await secretHashLogic.AddSecretHashAsync(recoveryCode, twoFactorAppRecoveryCode);
            user.TwoFactorAppRecoveryCode = recoveryCode;
            await tenantDataRepository.SaveAsync(user);

            logger.ScopeTrace(() => $"User '{user.Email}', two-factor app secret set.", triggerEvent: true);
            return user;
        }

        public async Task<User> ValidateTwoFactorAppRecoveryCodeUser(string email, string twoFactorAppRecoveryCode)
        {
            email = email?.ToLowerInvariant();
            logger.ScopeTrace(() => $"Validating two-factor app recovery code user '{email}', Route '{RouteBinding?.Route}'.");

            var failingTwoFactorCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email, FailingLoginTypes.TwoFactorAuthenticator);

            var user = await accountLogic.GetUserAsync(email);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{user.Email}' do not exist or is disabled, trying to validate two-factor app recovery code.");
            }

            if (user.TwoFactorAppRecoveryCode == null)
            {
                throw new InvalidOperationException($"User '{user.Email}' do not have a two-factor app recovery code, trying to validate two-factor app recovery code.");
            }

            if (await secretHashLogic.ValidateSecretAsync(user.TwoFactorAppRecoveryCode, twoFactorAppRecoveryCode))
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(email, FailingLoginTypes.TwoFactorAuthenticator);
                logger.ScopeTrace(() => $"User '{email}' two-factor app recovery code is valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingTwoFactorCount), triggerEvent: true);
                return user;
            }
            else
            {
                var increasedTwoFactorCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email, FailingLoginTypes.TwoFactorAuthenticator);
                logger.ScopeTrace(() => $"Failing two-factor count increased for user '{email}', two-factor app recovery code invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedTwoFactorCount), triggerEvent: true);
                throw new InvalidRecoveryCodeException($"Two-factor app recovery code invalid, user '{email}'.");
            }
        }
    }
}
