using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.ViewModels;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class AccountActionLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        protected readonly TelemetryScopedLogger logger;
        private readonly SequenceLogic sequenceLogic;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly IStringLocalizer localizer;
        private readonly ITenantRepository tenantRepository;
        private readonly SecretHashLogic secretHashLogic;
        private readonly AccountLogic accountLogic;
        private readonly FailingLoginLogic failingLoginLogic;
        private readonly SendEmailLogic sendEmailLogic;

        public AccountActionLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, SequenceLogic sequenceLogic, IConnectionMultiplexer redisConnectionMultiplexer, IStringLocalizer localizer, ITenantRepository tenantRepository, SecretHashLogic secretHashLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, SendEmailLogic sendEmailLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.sequenceLogic = sequenceLogic;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.localizer = localizer;
            this.tenantRepository = tenantRepository;
            this.secretHashLogic = secretHashLogic;
            this.accountLogic = accountLogic;
            this.failingLoginLogic = failingLoginLogic;
            this.sendEmailLogic = sendEmailLogic;
        }

        public async Task<EmailConfirmationCodeSendStatus> SendEmailConfirmationCodeAsync(string email, bool forceNewCode)
        {
            var db = redisConnectionMultiplexer.GetDatabase();
            var key = EmailConfirmationCodeRadisKey(email);
            if (!forceNewCode && await db.KeyExistsAsync(key))
            {
                return EmailConfirmationCodeSendStatus.UseExistingCode;
            }
            else
            {
                await SaveAndSendEmailConfirmationCode(db, key, email);
                return forceNewCode ? EmailConfirmationCodeSendStatus.ForceNewCode : EmailConfirmationCodeSendStatus.NewCode;
            }
        }

        private async Task SaveAndSendEmailConfirmationCode(IDatabase redisDb, string redisKey, string email)
        {
            var confirmationCode = RandomGenerator.GenerateCode(Constants.Models.User.EmailConfirmationCodeLength).ToUpper();
            var confirmationCodeObj = new EmailConfirmationCode();
            await secretHashLogic.AddSecretHashAsync(confirmationCodeObj, confirmationCode);
            await redisDb.StringSetAsync(redisKey, confirmationCodeObj.ToJson(), TimeSpan.FromSeconds(settings.EmailConfirmationCodeLifetime));

            var user = await accountLogic.GetUserAsync(email);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{user.Email}' do not exist or is disabled, trying to send email confirmation code.");
            }

            var emailSubject = $"{(RouteBinding.DisplayName.IsNullOrWhiteSpace() ? string.Empty : $"{RouteBinding.DisplayName} - ")}{localizer["Email confirmation"]}";
            var emailBody = localizer["<p>Your{0}email confirmation code: {1}</p>", RouteBinding.DisplayName.IsNullOrWhiteSpace() ? " " : $" {RouteBinding.DisplayName} ", confirmationCode];
            await sendEmailLogic.SendEmailAsync(new MailAddress(user.Email, GetDisplayName(user)), emailSubject, emailBody, fromName: RouteBinding.DisplayName);

            logger.ScopeTrace(() => $"Confirmation email send to '{user.Email}' for user id '{user.UserId}'.", triggerEvent: true);
        }

        public async Task<User> VerifyEmailConfirmationCodeAsync(string email, string code)
        {
            var failingConfirmatioCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email);

            var db = redisConnectionMultiplexer.GetDatabase();
            var key = EmailConfirmationCodeRadisKey(email);
            var confirmationCodeValue = (string)await db.StringGetAsync(key);
            if (!confirmationCodeValue.IsNullOrEmpty())
            {
                var confirmationCode = confirmationCodeValue.ToObject<EmailConfirmationCode>();
                if (await secretHashLogic.ValidateSecretAsync(confirmationCode, code.ToUpper()))
                {
                    await failingLoginLogic.ResetFailingLoginCountAsync(email);
                    await db.KeyDeleteAsync(key);

                    var user = await accountLogic.GetUserAsync(email);
                    if (user == null || user.DisableAccount)
                    {
                        throw new UserNotExistsException($"User '{user.Email}' do not exist or is disabled, trying to do email confirmation.");
                    }
                    if (!user.EmailVerified)
                    {
                        user.EmailVerified = true;
                        await tenantRepository.SaveAsync(user);
                    }
                    logger.ScopeTrace(() => $"User email '{user.Email}' confirmed for user id '{user.UserId}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                    return user;
                }
                else
                {
                    var increasedfailingConfirmationCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(email);
                    logger.ScopeTrace(() => $"Failing confirmation count increased for user '{email}', confirmation code invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingConfirmationCount), triggerEvent: true);
                    throw new InvalidConfirmationCodeException($"Invalid confirmation code, user '{email}'.");
                }
            }
            else
            {
                logger.ScopeTrace(() => $"There is not a email confirmation code to compare with, user '{email}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                await SaveAndSendEmailConfirmationCode(db, key, email);
                throw new EmailConfirmationCodeNotExistsException("Email confirmation code not found."); 
            }
        }

        public async Task SendResetPasswordEmailAsync(string email)
        {
            logger.ScopeTrace(() => $"Send reset password email to '{email}'.");

            try
            {
                var user = await accountLogic.GetUserAsync(email);
                if (user == null || user.DisableAccount)
                {
                    throw new ResetPasswordException($"User with email '{email}' is not verified.");
                }

                (var separateSequenceString, var separateSequence) = await sequenceLogic.StartSeparateSequenceAsync(accountAction: true, currentSequence: Sequence, requireeUiUpPartyId: true);
                await sequenceLogic.SaveSequenceDataAsync(new ResetPasswordSequenceData
                {
                    Email = user.Email,
                    PasswordHash = PasswordHashToSha256(user)
                }, separateSequence);

                var confirmationUrl = UrlCombine.Combine(HttpContext.GetHostWithTenantAndTrack(), $"({RouteBinding.UpParty.Name})/action/resetpassword/_{separateSequenceString}");
                await sendEmailLogic.SendEmailAsync(new MailAddress(user.Email, GetDisplayName(user)),
                    localizer["Your password reset request"],
                    localizer["<h2 style='margin-bottom:30px;font-weight:300;line-height:1.5;font-size:24px'>Your password reset request</h2><p style='margin-bottom:30px'>Click on this <a href='{0}'>link</a> to reset your password.</p>", confirmationUrl]);

                logger.ScopeTrace(() => $"Reset password send to '{user.Email}' for user id '{user.UserId}'.", triggerEvent: true);
            }
            catch (ResetPasswordException ex)
            {
                logger.Error(ex);
            }
        }

        public async Task<(bool verified, User user)> VerifyResetPasswordAsync()
        {
            try
            {
                try
                {
                    var sequenceData = await sequenceLogic.GetSequenceDataAsync<ResetPasswordSequenceData>(remove: false);
                    logger.ScopeTrace(() => $"Verify reset password email '{sequenceData.Email}'.");

                    var user = await accountLogic.GetUserAsync(sequenceData.Email);
                    if (user == null || user.DisableAccount)
                    {
                        throw new ResetPasswordException($"User with email '{sequenceData.Email}' do not exists or is disabled.");
                    }

                    if (!sequenceData.PasswordHash.Equals(PasswordHashToSha256(user), StringComparison.Ordinal))
                    {
                        throw new ResetPasswordException($"The request is invalid because the user with email '{sequenceData.Email}' has changed password.");
                    }
                    logger.ScopeTrace(() => $"User is approved for reset password with email '{user.Email}' and id '{user.UserId}'.", triggerEvent: true);
                    if (!user.EmailVerified)
                    {
                        user.EmailVerified = true;
                        await tenantRepository.SaveAsync(user);
                        logger.ScopeTrace(() => $"User confirmation in verify reset password with email '{user.Email}' and id '{user.UserId}'.", triggerEvent: true);
                    }
                    return (verified: true, user: user);
                }
                catch (SequenceException sexc)
                {
                    throw new ResetPasswordException("Unable to read reset password sequence data. Maybe the link have been used before.", sexc);
                }
            }
            catch (ResetPasswordException ex)
            {
                logger.Error(ex);
                return (verified: false, user: null);
            }
        }
        public async Task RemoveResetPasswordSequenceDataAsync()
        {
            await sequenceLogic.RemoveSequenceDataAsync<ResetPasswordSequenceData>();
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

        private string PasswordHashToSha256(User user)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{user.HashAlgorithm}.{user.Hash}.{user.HashSalt}"));
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private string EmailConfirmationCodeRadisKey(string email)
        {
            return $"email_confirmation_code_{RouteBinding.TenantNameDotTrackName}_{email}";
        }      
    }
}
