using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class AccountLogic : BaseAccountLogic
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ExternalPasswordConnectLogic externalPasswordConnectLogic;
        private readonly FailingLoginLogic failingLoginLogic;
        private readonly PlanUsageLogic planUsageLogic;

        public AccountLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, IMasterDataRepository masterDataRepository, SecretHashLogic secretHashLogic, ExternalPasswordConnectLogic externalPasswordConnectLogic, FailingLoginLogic failingLoginLogic, PlanUsageLogic planUsageLogic, IHttpContextAccessor httpContextAccessor) : base(logger, tenantDataRepository, masterDataRepository, secretHashLogic, httpContextAccessor)
        {
            this.serviceProvider = serviceProvider;
            this.externalPasswordConnectLogic = externalPasswordConnectLogic;
            this.failingLoginLogic = failingLoginLogic;
            this.planUsageLogic = planUsageLogic;
        }

        public async Task<User> GetUserAsync(string userIdentifier)
        {
            userIdentifier = userIdentifier?.Trim()?.ToLower();
            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier });
            return await tenantDataRepository.GetAsync<User>(id, required: false, queryAdditionalIds: true);
        }

        public async Task<User> ValidateUser(string userIdentifier, string password, bool passwordlessSendCodeEnabled)
        {
            userIdentifier = userIdentifier?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Validating user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier });
            var user = await tenantDataRepository.GetAsync<User>(id, required: false, queryAdditionalIds: true);

            var failingLoginCount = await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, FailingLoginTypes.InternalLogin, sendingCode: passwordlessSendCodeEnabled);

            if (user == null || user.DisableAccount || user.Hash.IsNullOrWhiteSpace())
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginOrSendingCountAsync(userIdentifier, FailingLoginTypes.InternalLogin);
                logger.ScopeTrace(() => $"Failing login count increased for not existing user '{userIdentifier}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(password);
                throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled or do not have a password."); // UI message: Wrong email or password / Your email was not found
            }

            logger.ScopeTrace(() => $"User '{userIdentifier}' exists, with user id '{user.UserId}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount));
            if (await secretHashLogic.ValidateSecretAsync(user, password))
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(userIdentifier, FailingLoginTypes.InternalLogin);
                if (user.ChangePassword)
                {
                    logger.ScopeTrace(() => $"User '{userIdentifier}' and password valid, user have to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                    throw new ChangePasswordException($"Change password, user '{userIdentifier}'.");
                }
                else
                {
                    logger.ScopeTrace(() => $"User '{userIdentifier}' and password valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                    try
                    {
                        await ValidatePasswordRiskAsync(password);
                    }
                    catch
                    {
                        logger.ScopeTrace(() => $"User '{userIdentifier}' password is in risk based on global password breaches, user have to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);
                        throw;
                    }
                    return user;
                }
            }
            else
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginOrSendingCountAsync(userIdentifier, FailingLoginTypes.InternalLogin);
                logger.ScopeTrace(() => $"Failing login count increased for user '{userIdentifier}', password invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                throw new InvalidPasswordException($"Password invalid, user '{userIdentifier}'."); // UI message: Wrong email or password / Wrong password
            }
        }

        public async Task<User> ValidateUserChangePassword(string userIdentifier, string currentPassword, string newPassword, bool passwordlessSendCodeEnabled)
        {
            userIdentifier = userIdentifier?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Change password user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = userIdentifier });
            var user = await tenantDataRepository.GetAsync<User>(id, required: false, queryAdditionalIds: true);

            var failingLoginCount = await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, FailingLoginTypes.InternalLogin, sendingCode: passwordlessSendCodeEnabled);

            if (user == null || user.DisableAccount || user.Hash.IsNullOrWhiteSpace())
            {
                var increasedfailingLoginCount = await failingLoginLogic.IncreaseFailingLoginOrSendingCountAsync(userIdentifier, FailingLoginTypes.InternalLogin);
                logger.ScopeTrace(() => $"Failing login count increased for not existing user '{userIdentifier}', trying to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingLoginCount), triggerEvent: true);
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(currentPassword);
                throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled or do not have a password, trying to change password.");
            }

            logger.ScopeTrace(() => $"User '{userIdentifier}' exists, with user id '{user.UserId}', trying to change password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount));
            if (await secretHashLogic.ValidateSecretAsync(user, currentPassword))
            {
                await failingLoginLogic.ResetFailingLoginCountAsync(userIdentifier, FailingLoginTypes.InternalLogin);
                logger.ScopeTrace(() => $"User '{userIdentifier}', current password valid, changing password.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);

                if (currentPassword.Equals(newPassword, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NewPasswordEqualsCurrentException($"New password equals current password, user '{userIdentifier}'.");
                }

                await ValidatePasswordPolicyAndNotifyAsync(new UserIdentifier { Email = user.Email, Phone = user.Phone, Username = user.Username }, newPassword);

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

        protected override async Task ValidatePasswordPolicyAndNotifyAsync(UserIdentifier userIdentifier, string password)
        {
            await base.ValidatePasswordPolicyAndNotifyAsync(userIdentifier, password);

            if (RouteBinding?.ExternalPassword?.EnabledValidation == true)
            {
                await externalPasswordConnectLogic.ValidatePasswordAsync(userIdentifier, password);
            }

            if (RouteBinding?.ExternalPassword?.EnabledNotification == true)
            {
                await externalPasswordConnectLogic.PasswordNotificationAsync(userIdentifier, password);
            }
        }

        public async Task SendPhonePasswordlessCodeSmsAsync(string userIdentifier)
        {
            userIdentifier = userIdentifier?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Send passwordless code SMS for user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            await planUsageLogic.VerifyCanSendSmsAsync();

            await GetAccountActionLogicLogic().SendPhonePasswordlessCodeSmsAsync(userIdentifier);
        }

        public async Task<User> VerifyPhonePasswordlessCodeSmsAsync(string userIdentifier, string code)
        {
            userIdentifier = userIdentifier?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Verify passwordless code SMS for user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            return await GetAccountActionLogicLogic().VerifyPhonePasswordlessCodeSmsAsync(userIdentifier, code);
        }

        public async Task SendEmailPasswordlessCodeSmsAsync(string userIdentifier)
        {
            userIdentifier = userIdentifier?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Send passwordless code email for user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            await planUsageLogic.VerifyCanSendEmailAsync();

            await GetAccountActionLogicLogic().SendEmailPasswordlessCodeAsync(userIdentifier);
        }

        public async Task<User> VerifyEmailPasswordlessCodeSmsAsync(string userIdentifier, string code)
        {
            userIdentifier = userIdentifier?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Verify passwordless code email for user '{userIdentifier}', Route '{RouteBinding?.Route}'.");

            return await GetAccountActionLogicLogic().VerifyEmailPasswordlessCodeAsync(userIdentifier, code);
        }

        private AccountActionLogic GetAccountActionLogicLogic() => serviceProvider.GetService<AccountActionLogic>();

    }
}
