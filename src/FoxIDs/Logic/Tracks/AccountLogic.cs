using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class AccountLogic : BaseAccountLogic
    {
        private readonly FailingLoginLogic failingLoginLogic;

        public AccountLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IMasterDataRepository masterDataRepository, SecretHashLogic secretHashLogic, FailingLoginLogic failingLoginLogic, IHttpContextAccessor httpContextAccessor) : base(logger, tenantDataRepository, masterDataRepository, secretHashLogic, httpContextAccessor)
        {
            this.failingLoginLogic = failingLoginLogic;
        }

        public async Task<User> GetUserAsync(string email)
        {
            email = email?.ToLowerInvariant();
            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email });
            return await tenantDataRepository.GetAsync<User>(id, required: false);
        }

        public async Task<User> ValidateUser(string email, string password)
        {
            email = email?.ToLowerInvariant();
            logger.ScopeTrace(() => $"Validating user '{email}', Route '{RouteBinding?.Route}'.");

            ValidateEmail(email);
            var failingLoginCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email);

            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email });
            var user = await tenantDataRepository.GetAsync<User>(id, required: false);

            if (user == null || user.DisableAccount)
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email);
                logger.ScopeTrace(() => $"Failing login count increased for not existing user '{email}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(password);
                throw new UserNotExistsException($"User '{email}' do not exist or is disabled."); // UI message: Wrong email or password / Your email was not found
            }

            logger.ScopeTrace(() => $"User '{email}' exists, with user id '{user.UserId}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount));
            if (await secretHashLogic.ValidateSecretAsync(user, password))
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(email);
                if (user.ChangePassword)
                {
                    logger.ScopeTrace(() => $"User '{email}' and password valid, user have to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                    throw new ChangePasswordException($"Change password, user '{email}'.");
                }
                else
                {
                    logger.ScopeTrace(() => $"User '{email}' and password valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                    return user;
                }
            }
            else
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email);
                logger.ScopeTrace(() => $"Failing login count increased for user '{email}', password invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                throw new InvalidPasswordException($"Password invalid, user '{email}'."); // UI message: Wrong email or password / Wrong password
            }
        }

        public override async Task<User> ChangePasswordUser(string email, string currentPassword, string newPassword)
        {
            email = email?.ToLowerInvariant();
            logger.ScopeTrace(() => $"Change password user '{email}', Route '{RouteBinding?.Route}'.");

            ValidateEmail(email);
            var failingLoginCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email);

            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email });
            var user = await tenantDataRepository.GetAsync<User>(id, required: false);

            if (user == null || user.DisableAccount)
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email);
                logger.ScopeTrace(() => $"Failing login count increased for not existing user '{email}', trying to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(currentPassword);
                throw new UserNotExistsException($"User '{email}' do not exist or is disabled, trying to change password.");
            }

            logger.ScopeTrace(() => $"User '{email}' exists, with user id '{user.UserId}', trying to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount));
            if (await secretHashLogic.ValidateSecretAsync(user, currentPassword))
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(email);
                logger.ScopeTrace(() => $"User '{email}', current password valid, changing password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);

                if (currentPassword.Equals(newPassword, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NewPasswordEqualsCurrentException($"New password equals current password, user '{email}'.");
                }

                await ValidatePasswordPolicy(email, newPassword);

                await secretHashLogic.AddSecretHashAsync(user, newPassword);
                user.ChangePassword = false;
                await tenantDataRepository.SaveAsync(user);

                logger.ScopeTrace(() => $"User '{email}', password changed.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                return user;
            }
            else
            {
                throw new InvalidPasswordException($"Current password invalid, user '{email}'.");
            }
        }
    }
}
