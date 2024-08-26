﻿using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Logic;
using FoxIDs.Models.ViewModels;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class AccountActionLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        protected readonly TelemetryScopedLogger logger;
        private readonly ICacheProvider cacheProvider;
        private readonly IStringLocalizer localizer;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SecretHashLogic secretHashLogic;
        private readonly AccountLogic accountLogic;
        private readonly FailingLoginLogic failingLoginLogic;
        private readonly SendEmailLogic sendEmailLogic;

        public AccountActionLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ICacheProvider cacheProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, SecretHashLogic secretHashLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, SendEmailLogic sendEmailLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.secretHashLogic = secretHashLogic;
            this.accountLogic = accountLogic;
            this.failingLoginLogic = failingLoginLogic;
            this.sendEmailLogic = sendEmailLogic;
        }

        public Task<ConfirmationCodeSendStatus> SendEmailConfirmationCodeAsync(string email, bool forceNewCode)
        {
            email = email?.ToLowerInvariant();
            return SendEmailCodeAsync(GetEmailConfirmationCodeEmailContent(), EmailConfirmationCodeRedisKeyElement, email, forceNewCode, "email");
        }

        public Task<User> VerifyEmailConfirmationCodeAsync(string email, string code)
        {
            email = email?.ToLowerInvariant();
            return VerifyEmailCodeAsync(GetEmailConfirmationCodeEmailContent(), EmailConfirmationCodeRedisKeyElement, null, email, code, "email");
        }

        private string EmailConfirmationCodeRedisKeyElement => "email_confirmation_code";

        private Func<string, EmailContent> GetEmailConfirmationCodeEmailContent()
        {
            return (code) => new EmailContent
            {
                Subject = localizer["{0}Email confirmation", RouteBinding.DisplayName.IsNullOrWhiteSpace() ? string.Empty : $"{RouteBinding.DisplayName} - "],
                Body = localizer["Your{0}email confirmation code: {1}", RouteBinding.DisplayName.IsNullOrWhiteSpace() ? " " : $" {RouteBinding.DisplayName} ", code]
            };
        }

        public Task<ConfirmationCodeSendStatus> SendResetPasswordCodeAsync(string email, bool forceNewCode)
        {
            email = email?.ToLowerInvariant();
            return SendEmailCodeAsync(GetResetPasswordCodeEmailContent(), ResetPasswordCodeRedisKeyElement, email, forceNewCode, "reset password");
        }

        public Task<User> VerifyResetPasswordCodeAndSetPasswordAsync(string email, string code, string newPassword)
        {
            email = email?.ToLowerInvariant();
            Func<User, Task> onSuccess = (user) => accountLogic.SetPasswordUser(user, newPassword);
            return VerifyEmailCodeAsync(GetResetPasswordCodeEmailContent(), ResetPasswordCodeRedisKeyElement, onSuccess, email, code, "reset password");
        }

        private string ResetPasswordCodeRedisKeyElement => "reset_password_code";

        private Func<string, EmailContent> GetResetPasswordCodeEmailContent()
        {
            return (code) => new EmailContent
            {
                Subject = localizer["{0}Reset password", RouteBinding.DisplayName.IsNullOrWhiteSpace() ? string.Empty : $"{RouteBinding.DisplayName} - "],
                Body = localizer["Your{0}reset password confirmation code: {1}", RouteBinding.DisplayName.IsNullOrWhiteSpace() ? " " : $" {RouteBinding.DisplayName} ", code]
            };
        }

        private async Task<ConfirmationCodeSendStatus> SendEmailCodeAsync(Func<string, EmailContent> emailContent, string redisKeyElement, string email, bool forceNewCode, string logText)
        {
            var key = EmailConfirmationCodeCacheKey(redisKeyElement, email);
            if (!forceNewCode && await cacheProvider.ExistsAsync(key))
            {
                return ConfirmationCodeSendStatus.UseExistingCode;
            }
            else
            {
                await SaveAndSendEmailCode(key, email, emailContent, logText);
                return forceNewCode ? ConfirmationCodeSendStatus.ForceNewCode : ConfirmationCodeSendStatus.NewCode;
            }
        }

        private async Task SaveAndSendEmailCode(string redisKey, string email, Func<string, EmailContent> emailContent, string logText)
        {
            var confirmationCode = RandomGenerator.GenerateCode(Constants.Models.User.ConfirmationCodeLength).ToUpper();
            var confirmationCodeObj = new ConfirmationCode();
            await secretHashLogic.AddSecretHashAsync(confirmationCodeObj, confirmationCode);
            await cacheProvider.SetAsync(redisKey, confirmationCodeObj.ToJson(), settings.ConfirmationCodeLifetime);

            var user = await accountLogic.GetUserAsync(email);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{email}' do not exist or is disabled, trying to send {logText} confirmation code.");
            }

            await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), user.Email), emailContent(confirmationCode), fromName: RouteBinding.DisplayName);

            logger.ScopeTrace(() => $"Email with {logText} confirmation code send to '{user.Email}' for user id '{user.UserId}'.", triggerEvent: true);
        }

        private async Task<User> VerifyEmailCodeAsync(Func<string, EmailContent> emailContent, string redisKeyElement, Func<User, Task> onSuccess, string email, string code, string logText)
        {
            var failingConfirmatioCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email);

            var key = EmailConfirmationCodeCacheKey(redisKeyElement, email);
            var confirmationCodeValue = await cacheProvider.GetAsync(key);
            if (!confirmationCodeValue.IsNullOrEmpty())
            {
                var confirmationCode = confirmationCodeValue.ToObject<ConfirmationCode>();
                if (await secretHashLogic.ValidateSecretAsync(confirmationCode, code.ToUpper()))
                {
                    await failingLoginLogic.ResetFailingLoginCountAsync(email);

                    var user = await accountLogic.GetUserAsync(email);
                    if (user == null || user.DisableAccount)
                    {
                        throw new UserNotExistsException($"User '{user.Email}' do not exist or is disabled, trying to do {logText} confirmation code.");
                    }
                    if (!user.EmailVerified)
                    {
                        user.EmailVerified = true;
                        await tenantDataRepository.SaveAsync(user);
                    }
                    if (onSuccess != null)
                    {
                        await onSuccess(user);
                    }

                    await cacheProvider.DeleteAsync(key);
                    logger.ScopeTrace(() => $"User '{user.Email}' {logText} confirmation code verified for user id '{user.UserId}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                    return user;
                }
                else
                {
                    var increasedfailingConfirmationCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email);
                    logger.ScopeTrace(() => $"Failing count increased for user '{email}', {logText} confirmation code invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingConfirmationCount), triggerEvent: true);
                    throw new InvalidCodeException($"Invalid {logText} confirmation code, user '{email}'.");
                }
            }
            else
            {
                logger.ScopeTrace(() => $"There is not a email code to compare with, user '{email}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                await SaveAndSendEmailCode(key, email, emailContent, logText);
                throw new CodeNotExistsException($"{logText} confirmation code not found."); 
            }
        }

        private string GetDisplayName(User user)
        {
            var displayName = user.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.Name);
            if (displayName.IsNullOrWhiteSpace())
            {
                var nameList = new List<string>();

                var givenName = user.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.GivenName);
                if (!givenName.IsNullOrWhiteSpace()) nameList.Add(givenName);

                var middleName = user.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.MiddleName);
                if (!middleName.IsNullOrWhiteSpace()) nameList.Add(middleName);

                var familyName = user.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.FamilyName);
                if (!familyName.IsNullOrWhiteSpace()) nameList.Add(familyName);

                displayName = string.Join(" ", nameList);
            }
            return displayName;
        }

        private string EmailConfirmationCodeCacheKey(string keyElement, string email)
        {
            return $"{keyElement}_{RouteBinding.TenantNameDotTrackName}_{email}";
        }      
    }
}