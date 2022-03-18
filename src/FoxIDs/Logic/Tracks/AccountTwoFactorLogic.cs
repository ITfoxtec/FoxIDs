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
    public class AccountTwoFactorLogic : LogicBase
    {
        private const int secretAndRecoveryCodeLength = 30;

        protected readonly TelemetryScopedLogger logger;
        protected readonly ITenantRepository tenantRepository;
        protected readonly SecretHashLogic secretHashLogic;

        public AccountTwoFactorLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, SecretHashLogic secretHashLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.secretHashLogic = secretHashLogic;
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
        
        public async Task ValidateTwoFactorAsync(string email, string secret, string appCode)
        {
            var twoFactor = new TwoFactorAuthenticator();
            bool isValid = await Task.FromResult(twoFactor.ValidateTwoFactorPIN(secret, appCode, false));

            if (!isValid)
            {
                throw new InvalidAppCodeException($"Invalid two-factor app code, user '{email}'.");
            }
        }

        public string CreateRecoveryCode()
        {
            return Base32Encoding.ToString(RandomGenerator.GenerateBytes(secretAndRecoveryCodeLength)).TrimEnd('=');
        }

        public async Task<User> SetTwoFactorAppSecretUser(string email, string twoFactorAppSecret, string twoFactorAppRecoveryCode)
        {
            logger.ScopeTrace(() => $"Set two-factor app secret user '{email}', Route '{RouteBinding?.Route}'.");

            var id = await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email });
            var user = await tenantRepository.GetAsync<User>(id, required: false);

            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{user.Email}' do not exist or is disabled, trying to set two-factor app secret.");
            }

            user.TwoFactorAppSecret = twoFactorAppSecret;
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

            var id = await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email });
            var user = await tenantRepository.GetAsync<User>(id, required: false);

            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{user.Email}' do not exist or is disabled, trying to validate two-factor app recovery code.");
            }

            if (user.TwoFactorAppRecoveryCode == null)
            {
                throw new UserNotExistsException($"User '{user.Email}' do not have a two-factor app recovery code, trying to validate two-factor app recovery code.");
            }

            if (await secretHashLogic.ValidateSecretAsync(user.TwoFactorAppRecoveryCode, twoFactorAppRecoveryCode))
            {
                logger.ScopeTrace(() => $"User '{email}' two-factor app recovery code is valid.", triggerEvent: true);
                return user;
            }
            else
            {
                throw new InvalidRecoveryCodeException($"Two-factor app recovery code invalid, user '{email}'.");
            }
        }
    }
}
