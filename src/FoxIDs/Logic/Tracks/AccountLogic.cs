using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
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

        public async Task<User> GetUserAsync(string userIdentifier)
        {
            userIdentifier = userIdentifier?.ToLowerInvariant();
            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier });
            return await tenantDataRepository.GetAsync<User>(id, required: false, queryAdditionalIds: true);
        }

        public async Task<User> ValidateUser(string userIdentifier, string password)
        {
            userIdentifier = userIdentifier?.ToLowerInvariant();
            logger.ScopeTrace(() => $"Validating user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier });
            var user = await tenantDataRepository.GetAsync<User>(id, required: false, queryAdditionalIds: true);

            var failingLoginCount = await failingLoginLogic.VerifyFailingLoginCountAsync(GetFailingLoginUserId(user, userIdentifier), FailingLoginTypes.Login);

            if (user == null || user.DisableAccount)
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(GetFailingLoginUserId(user, userIdentifier), FailingLoginTypes.Login);
                logger.ScopeTrace(() => $"Failing login count increased for not existing user '{userIdentifier}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(password);
                throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled."); // UI message: Wrong email or password / Your email was not found
            }

            logger.ScopeTrace(() => $"User '{userIdentifier}' exists, with user id '{user.UserId}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount));
            if (await secretHashLogic.ValidateSecretAsync(user, password))
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(GetFailingLoginUserId(user, userIdentifier), FailingLoginTypes.Login);
                if (user.ChangePassword)
                {
                    logger.ScopeTrace(() => $"User '{userIdentifier}' and password valid, user have to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                    throw new ChangePasswordException($"Change password, user '{userIdentifier}'.");
                }
                else
                {
                    logger.ScopeTrace(() => $"User '{userIdentifier}' and password valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                    return user;
                }
            }
            else
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(GetFailingLoginUserId(user, userIdentifier), FailingLoginTypes.Login);
                logger.ScopeTrace(() => $"Failing login count increased for user '{userIdentifier}', password invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                throw new InvalidPasswordException($"Password invalid, user '{userIdentifier}'."); // UI message: Wrong email or password / Wrong password
            }
        }

        public async Task<User> ValidateUserChangePassword(string userIdentifier, string currentPassword, string newPassword)
        {
            userIdentifier = userIdentifier?.ToLowerInvariant();
            logger.ScopeTrace(() => $"Change password user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier });
            var user = await tenantDataRepository.GetAsync<User>(id, required: false, queryAdditionalIds: true);

            var failingLoginCount = await failingLoginLogic.VerifyFailingLoginCountAsync(GetFailingLoginUserId(user, userIdentifier), FailingLoginTypes.Login);

            if (user == null || user.DisableAccount)
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(GetFailingLoginUserId(user, userIdentifier), FailingLoginTypes.Login);
                logger.ScopeTrace(() => $"Failing login count increased for not existing user '{userIdentifier}', trying to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(currentPassword);
                throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled, trying to change password.");
            }

            logger.ScopeTrace(() => $"User '{userIdentifier}' exists, with user id '{user.UserId}', trying to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount));
            if (await secretHashLogic.ValidateSecretAsync(user, currentPassword))
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(GetFailingLoginUserId(user, userIdentifier), FailingLoginTypes.Login);
                logger.ScopeTrace(() => $"User '{userIdentifier}', current password valid, changing password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);

                if (currentPassword.Equals(newPassword, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NewPasswordEqualsCurrentException($"New password equals current password, user '{userIdentifier}'.");
                }

                await ValidatePasswordPolicy(new UserIdentifier { Email = user.Email, Phone = user.Phone, Username = user.Username }, newPassword);

                await secretHashLogic.AddSecretHashAsync(user, newPassword);
                user.ChangePassword = false;
                await tenantDataRepository.SaveAsync(user);

                logger.ScopeTrace(() => $"User '{userIdentifier}', password changed.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                return user;
            }
            else
            {
                throw new InvalidPasswordException($"Current password invalid, user '{userIdentifier}'.");
            }
        }

        private static string GetFailingLoginUserId(User user, string userIdentifier) => user != null ? user.UserId : userIdentifier;
    }
}
