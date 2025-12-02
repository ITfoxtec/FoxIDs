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
                    var passwordPolicy = GetPasswordPolicy(user);
                    try
                    {
                        await ValidatePasswordPolicyAndNotifyAsync(new UserIdentifier { Email = user.Email, Phone = user.Phone, Username = user.Username }, password, PasswordState.Current, user, passwordPolicy);
                        return user;
                    }
                    catch (PasswordPolicyException pex)
                    {
                        if (CanUseSoftPasswordChange(user, passwordPolicy, pex))
                        {
                            throw new SoftChangePasswordException("Initiate password soft change.", pex) { PasswordPolicy = passwordPolicy };
                        }

                        throw;
                    }
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

                var passwordPolicy = GetPasswordPolicy(user);
                await ValidatePasswordPolicyAndNotifyAsync(new UserIdentifier { Email = user.Email, Phone = user.Phone, Username = user.Username }, newPassword, PasswordState.New, user, passwordPolicy);

                await UpdatePasswordHistoryAsync(user, currentPassword, passwordPolicy);
                await secretHashLogic.AddSecretHashAsync(user, newPassword);
                user.PasswordLastChanged = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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

        protected override async Task ValidatePasswordPolicyAndNotifyAsync(UserIdentifier userIdentifier, string password, PasswordState state, User user, PasswordPolicyState passwordPolicy)
        {
            await base.ValidatePasswordPolicyAndNotifyAsync(userIdentifier, password, state, user, passwordPolicy);

            var externalPassword = RouteBinding.ExternalPassword;
            if (externalPassword != null)
            {
                var doValidate = state switch
                {
                    PasswordState.Current => externalPassword.EnabledValidationCurrent ?? false,
                    PasswordState.New => externalPassword.EnabledValidationNew ?? false,
                    _ => false
                };
                if (doValidate)
                {
                    await externalPasswordConnectLogic.ValidatePasswordAsync(userIdentifier, password, state);
                }

                var doNotify = state switch
                {
                    PasswordState.Current => externalPassword.EnabledNotificationCurrent ?? false,
                    PasswordState.New => externalPassword.EnabledNotificationNew ?? false,
                    _ => false
                };
                if (doNotify)
                {
                    await externalPasswordConnectLogic.PasswordNotificationAsync(userIdentifier, password, state);
                }
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

        private bool CanUseSoftPasswordChange(User user, PasswordPolicyState policy, AccountException exception)
        {
            if (policy.SoftChange <= 0)
            {
                return false;
            }

            if (user.PasswordLastChanged <= 0)
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var allowedAge = policy.SoftChange;
            if (policy.MaxAge > 0 && exception is PasswordExpiredException)
            {
                allowedAge += policy.MaxAge;
            }

            return now <= user.PasswordLastChanged + allowedAge;
        }
    }
}