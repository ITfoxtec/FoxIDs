using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
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
    public class AccountActionLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        protected readonly TelemetryScopedLogger logger;
        private readonly SequenceLogic sequenceLogic;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly IStringLocalizer localizer;
        private readonly ITenantRepository tenantRepository;
        private readonly SendEmailLogic sendEmailLogic;

        public AccountActionLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, SequenceLogic sequenceLogic, IConnectionMultiplexer redisConnectionMultiplexer, IStringLocalizer localizer, ITenantRepository tenantRepository, SendEmailLogic sendEmailLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.sequenceLogic = sequenceLogic;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.localizer = localizer;
            this.tenantRepository = tenantRepository;
            this.sendEmailLogic = sendEmailLogic;
        }

        public async Task SendConfirmationEmailAsync(User user)
        {
            logger.ScopeTrace($"Send confirmation email to '{user.Email}' for user id '{user.UserId}'.");
            if (user == null || user.DisableAccount)
            {
                throw new ConfirmationException($"User with email '{user.Email}' do not exists or is disabled.");
            }
            if (user.EmailVerified)
            {
                logger.ScopeTrace($"User is confirmed, email '{user.Email}' and id '{user.UserId}'.");
                return;
            }

            var db = redisConnectionMultiplexer.GetDatabase();
            var key = ConfirmationEmailWaitPeriodRadisKey(user.Email);
            if (await db.KeyExistsAsync(key))
            {
                logger.ScopeTrace($"User confirmation wait period, email '{user.Email}' and id '{user.UserId}'.");
                return;
            }
            else
            {
                await db.StringSetAsync(key, true, TimeSpan.FromSeconds(settings.ConfirmationEmailWaitPeriod));
            }

            (var separateSequenceString, var separateSequence) = await sequenceLogic.StartSeparateSequenceAsync(accountAction: true, currentSequence: Sequence, requireeUiUpPartyId: true);
            await sequenceLogic.SaveSequenceDataAsync(new ConfirmationSequenceData
            {
                Email = user.Email
            }, separateSequence);

            var confirmationUrl = UrlCombine.Combine(HttpContext.GetHost(), $"{RouteBinding.TenantName}/{RouteBinding.TrackName}/({RouteBinding.UpParty.Name})/action/confirmation/_{separateSequenceString}");
            await sendEmailLogic.SendEmailAsync(new MailAddress(user.Email, GetDisplayName(user)),
                localizer["Please confirm your email address"], 
                localizer["<h2 style='margin-bottom:30px;font-weight:300;line-height:1.5;font-size:24px'>Please confirm your email address</h2><p style='margin-bottom:30px'>By clicking on this <a href='{0}'>link</a>, you are confirming your email address.</p>", confirmationUrl]);

            logger.ScopeTrace($"Confirmation send to '{user.Email}' for user id '{user.UserId}'.", triggerEvent: true);
        }

        public async Task<bool> VerifyConfirmationAsync()
        {
            try
            {
                try
                {
                    var sequenceData = await sequenceLogic.GetSequenceDataAsync<ConfirmationSequenceData>(remove: true);
                    logger.ScopeTrace($"Verify confirmation email '{sequenceData.Email}'.");

                    var id = await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = sequenceData.Email });
                    var user = await tenantRepository.GetAsync<User>(id, requered: false);
                    if (user == null || user.DisableAccount)
                    {
                        throw new ConfirmationException($"User with email '{sequenceData.Email}' do not exists or is disabled.");
                    }

                    var db = redisConnectionMultiplexer.GetDatabase();
                    await db.KeyDeleteAsync(ConfirmationEmailWaitPeriodRadisKey(user.Email));

                    if (!user.EmailVerified)
                    {
                        user.EmailVerified = true;
                        await tenantRepository.SaveAsync(user);
                        logger.ScopeTrace($"User confirmation with email '{user.Email}' and id '{user.UserId}'.", triggerEvent: true);
                    }
                    else
                    {
                        logger.ScopeTrace($"User re-confirmation with email '{user.Email}' and id '{user.UserId}'.", triggerEvent: true);
                    }
                    return true;
                }
                catch (SequenceException sexc)
                {
                    throw new ConfirmationException("Unable to read confirmation sequence data. Maybe the link have been used before.", sexc);
                }
            }
            catch (ConfirmationException ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        public async Task SendResetPasswordEmailAsync(string email)
        {
            logger.ScopeTrace($"Send reset password email to '{email}'.");

            try
            {
                var id = await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email });
                var user = await tenantRepository.GetAsync<User>(id, requered: false);
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

                var confirmationUrl = UrlCombine.Combine(HttpContext.GetHost(), $"{RouteBinding.TenantName}/{RouteBinding.TrackName}/({RouteBinding.UpParty.Name})/action/resetpassword/_{separateSequenceString}");
                await sendEmailLogic.SendEmailAsync(new MailAddress(user.Email, GetDisplayName(user)),
                    localizer["Your password reset request"],
                    localizer["<h2 style='margin-bottom:30px;font-weight:300;line-height:1.5;font-size:24px'>Your password reset request</h2><p style='margin-bottom:30px'>Click on this <a href='{0}'>link</a> to reset your password.</p>", confirmationUrl]);

                logger.ScopeTrace($"Reset password send to '{user.Email}' for user id '{user.UserId}'.", triggerEvent: true);
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
                    logger.ScopeTrace($"Verify reset password email '{sequenceData.Email}'.");

                    var id = await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = sequenceData.Email });
                    var user = await tenantRepository.GetAsync<User>(id, requered: false);
                    if (user == null || user.DisableAccount)
                    {
                        throw new ResetPasswordException($"User with email '{sequenceData.Email}' do not exists or is disabled.");
                    }

                    if (!sequenceData.PasswordHash.Equals(PasswordHashToSha256(user), StringComparison.Ordinal))
                    {
                        throw new ResetPasswordException($"The request is invalid because the user with email '{sequenceData.Email}' has changed password.");
                    }
                    logger.ScopeTrace($"User is approved for reset password with email '{user.Email}' and id '{user.UserId}'.", triggerEvent: true);
                    if (!user.EmailVerified)
                    {
                        user.EmailVerified = true;
                        await tenantRepository.SaveAsync(user);
                        logger.ScopeTrace($"User confirmation in verify reset password with email '{user.Email}' and id '{user.UserId}'.", triggerEvent: true);
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
            var displayName = user.Claims.FindFirstValue(c => c.Claim == JwtClaimTypes.Name);
            if (displayName.IsNullOrWhiteSpace())
            {
                var nameList = new List<string>();

                var givenName = user.Claims.FindFirstValue(c => c.Claim == JwtClaimTypes.GivenName);
                if (!givenName.IsNullOrWhiteSpace()) nameList.Add(givenName);

                var middleName = user.Claims.FindFirstValue(c => c.Claim == JwtClaimTypes.MiddleName);
                if (!middleName.IsNullOrWhiteSpace()) nameList.Add(middleName);

                var familyName = user.Claims.FindFirstValue(c => c.Claim == JwtClaimTypes.FamilyName);
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

        private string ConfirmationEmailWaitPeriodRadisKey(string email)
        {
            return $"confirmation_email_wait_period_{RouteBinding.TenantDashTrackName}_{email}";
        }
    }
}
