using FoxIDs.Models;
using Google.Authenticator;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using ITfoxtec.Identity;

namespace FoxIDs.Logic
{
    public class AccountTwoFactorLogic : LogicSequenceBase
    {
        private const int secretAndRecoveryCodeLength = 30;
        private const string secretName = "2fa";  

        protected readonly TelemetryScopedLogger logger;
        protected readonly ITenantRepository tenantRepository;
        private readonly ExternalSecretLogic externalSecretLogic;
        protected readonly SecretHashLogic secretHashLogic;
        private readonly AccountLogic accountLogic;
        private readonly FailingLoginLogic failingLoginLogic;

        public AccountTwoFactorLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, ExternalSecretLogic externalSecretLogic, SecretHashLogic secretHashLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.externalSecretLogic = externalSecretLogic;
            this.secretHashLogic = secretHashLogic;
            this.accountLogic = accountLogic;
            this.failingLoginLogic = failingLoginLogic;
        }

        public async Task<TwoFactorSetupInfo> GenerateSetupCodeAsync(string twoFactorAppName, string email)
        {
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
            var failingTwoFactorCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email);

            var twoFactor = new TwoFactorAuthenticator();
            bool isValid = await Task.FromResult(twoFactor.ValidateTwoFactorPIN(secret, appCode, false));

            if (isValid)
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(email);
                logger.ScopeTrace(() => $"User '{email}' two-factor app code is valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingTwoFactorCount), triggerEvent: true);
            }
            else 
            {
                var increasedTwoFactorCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email);
                logger.ScopeTrace(() => $"Failing two-factor count increased for user '{email}', two-factor app code invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedTwoFactorCount), triggerEvent: true);
                throw new InvalidAppCodeException($"Invalid two-factor app code, user '{email}'.");
            }
        }

        public async Task ValidateTwoFactorByExternalSecretAsync(string email, string secretExternalName, string appCode)
        {
            var secret = await externalSecretLogic.GetExternalSecretAsync(secretExternalName);
            if (secret.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException($"Unable to get external secret from Key Vault, {nameof(secretExternalName)} '{secretExternalName}'.");
            }

            await ValidateTwoFactorBySecretAsync(email, secret, appCode);
        }

        public string CreateRecoveryCode()
        {
            return Base32Encoding.ToString(RandomGenerator.GenerateBytes(secretAndRecoveryCodeLength)).TrimEnd('=');
        }

        public async Task<User> SetTwoFactorAppSecretUser(string email, string newSecret, string secretExternalName, string twoFactorAppRecoveryCode)
        {
            logger.ScopeTrace(() => $"Set two-factor app secret user '{email}', Route '{RouteBinding?.Route}'.");

            var user = await accountLogic.GetUserAsync(email);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{user.Email}' do not exist or is disabled, trying to set two-factor app secret.");
            }          

            if(!secretExternalName.IsNullOrEmpty())
            {
                user.TwoFactorAppSecretExternalName = await externalSecretLogic.SetExternalSecretByExternalNameAsync(secretExternalName, newSecret);
            }
            else
            {
                user.TwoFactorAppSecretExternalName = await externalSecretLogic.SetExternalSecretByNameAsync(secretName, newSecret);
            }

            var recoveryCode = new TwoFactorAppRecoveryCode();
            await secretHashLogic.AddSecretHashAsync(recoveryCode, twoFactorAppRecoveryCode);
            user.TwoFactorAppRecoveryCode = recoveryCode;
            await tenantRepository.SaveAsync(user);

            logger.ScopeTrace(() => $"User '{user.Email}', two-factor app secret set.", triggerEvent: true);
            return user;
        }

        public async Task<User> ValidateTwoFactorAppRecoveryCodeUser(string email, string twoFactorAppRecoveryCode)
        {
            logger.ScopeTrace(() => $"Validating two-factor app recovery code user '{email}', Route '{RouteBinding?.Route}'.");

            var failingTwoFactorCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email);

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
                await failingLoginLogic.ResetFailingLoginCountAsync(email);
                logger.ScopeTrace(() => $"User '{email}' two-factor app recovery code is valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingTwoFactorCount), triggerEvent: true);
                return user;
            }
            else
            {
                var increasedTwoFactorCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email);
                logger.ScopeTrace(() => $"Failing two-factor count increased for user '{email}', two-factor app recovery code invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedTwoFactorCount), triggerEvent: true);
                throw new InvalidRecoveryCodeException($"Two-factor app recovery code invalid, user '{email}'.");
            }
        }
    }
}
