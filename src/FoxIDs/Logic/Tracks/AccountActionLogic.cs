using FoxIDs.Infrastructure;
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
using System.Web;

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
        private readonly SendSmsLogic sendSmsLogic;
        private readonly SendEmailLogic sendEmailLogic;
        private readonly TrackCacheLogic trackCacheLogic;

        public AccountActionLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ICacheProvider cacheProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, SecretHashLogic secretHashLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, SendSmsLogic sendSmsLogic, SendEmailLogic sendEmailLogic, TrackCacheLogic trackCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.secretHashLogic = secretHashLogic;
            this.accountLogic = accountLogic;
            this.failingLoginLogic = failingLoginLogic;
            this.sendSmsLogic = sendSmsLogic;
            this.sendEmailLogic = sendEmailLogic;
            this.trackCacheLogic = trackCacheLogic;
        }

        public Task<ConfirmationCodeSendStatus> SendPhoneConfirmationCodeSmsAsync(string phone, bool forceNewCode)
        {
            phone = phone?.Trim();
            return SendCodeAsync(SmsConfirmationCodeKeyElement, phone, GetSmsSendAction(), forceNewCode, SmsConfirmationCodeLogText);
        }

        public Task<User> VerifyPhoneConfirmationCodeSmsAsync(string phone, string code)
        {
            return VerifyCodeAsync(SendType.Sms, SmsConfirmationCodeKeyElement, phone, code, GetSmsSendAction(), null, SmsConfirmationCodeLogText);
        }

        public Task<ConfirmationCodeSendStatus> SendEmailConfirmationCodeAsync(string email, bool forceNewCode)
        {
            email = email?.Trim()?.ToLower();
            return SendCodeAsync(EmailConfirmationCodeKeyElement, email, GetEmailSendAction(), forceNewCode, EmailConfirmationCodeLogText);
        }

        public Task<User> VerifyEmailConfirmationCodeAsync(string email, string code)
        {
            email = email?.ToLower();
            return VerifyCodeAsync(SendType.Email, EmailConfirmationCodeKeyElement, email, code, GetEmailSendAction(), null, EmailConfirmationCodeLogText);
        }

        private string SmsConfirmationCodeLogText => "Phone (SMS) confirmation code";
        private string SmsConfirmationCodeKeyElement => "sms_confirmation_code";

        private string EmailConfirmationCodeLogText => "Email confirmation code";
        private string EmailConfirmationCodeKeyElement => "email_confirmation_code";

        private SmsContent GetPhoneConfirmationCodeSms(string confirmationCode)
        {
            return new SmsContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Sms = localizer["Your{0}email confirmation code: {1}", $" {GetCompanyName()} ", confirmationCode]
            };
        }

        private EmailContent GetEmailConfirmationCodeEmailContent(string confirmationCode)
        {
            return new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0}Email confirmation", $"{GetCompanyName()} - "],
                Body = localizer["Your{0}email confirmation code: {1}", $" {GetCompanyName()} ", GetCodeHtml(confirmationCode)]
            };
        }

        private Func<User, string, Task> GetSmsSendAction()
        {
            return async (user, code) => await sendSmsLogic.SendSmsAsync(user.Phone, GetPhoneConfirmationCodeSms(code));
        }

        private Func<User, string, Task> GetEmailSendAction()
        {
            return async (user, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), user.Email), await AddAddressAndInfoAsync(GetEmailConfirmationCodeEmailContent(code)));
        }

        public Task<ConfirmationCodeSendStatus> SendResetPasswordCodeAsync(string email, bool forceNewCode)
        {
            email = email?.Trim()?.ToLower();
            return SendCodeAsync(EmailResetPasswordCodeKeyElement, email, GetEmailSendResetPasswordAction(), forceNewCode, EmailResetPasswordCodeLogText);
        }

        public Task<User> VerifyResetPasswordCodeAndSetPasswordAsync(string email, string code, string newPassword)
        {
            email = email?.ToLower();
            Func<User, Task> onSuccess = (user) => accountLogic.SetPasswordUser(user, newPassword);
            return VerifyCodeAsync(SendType.Email, EmailResetPasswordCodeKeyElement, email, code, GetEmailSendResetPasswordAction(), onSuccess, EmailResetPasswordCodeLogText);
        }

        private string EmailResetPasswordCodeLogText => "Email reset password code";
        private string EmailResetPasswordCodeKeyElement => "reset_password_code";

        private EmailContent GetResetPasswordCodeEmailContent(string confirmationCode)
        {
            return new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0}Reset password", $"{GetCompanyName()} - "],
                Body = localizer["Your{0}reset password confirmation code: {1}", $" {GetCompanyName()} ", GetCodeHtml(confirmationCode)]
            };
        }

        private Func<User, string, Task> GetEmailSendResetPasswordAction()
        {
            return async (user, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), user.Email), await AddAddressAndInfoAsync(GetResetPasswordCodeEmailContent(code)));
        }

        private string GetCompanyName()
        {
            return HttpUtility.HtmlEncode(RouteBinding.CompanyName.IsNullOrWhiteSpace() ? "FoxIDs" : RouteBinding.CompanyName);
        }

        private string GetCodeHtml(string code)
        {
            var codeHtml = string.Format(
@"<table border=""0"" cellpadding=""5"" cellspacing=""0"" width=""100%"">
  <tbody>
    <tr><td style=""height: 20px;"">&nbsp;</td></tr>
      <tr>
        <td>
          <div align=""center"">
            <strong>{0}</strong>
        </div>
    </td>
    </tr>
    <tr><td style=""height: 20px;"">&nbsp;</td></tr>
</tbody>
</table>", code);
            return codeHtml;
        }

        private async Task<ConfirmationCodeSendStatus> SendCodeAsync(string keyElement, string userIdentifier, Func<User, string, Task> sendActionAsync, bool forceNewCode, string logText)
        {
            var key = CodeCacheKey(keyElement, userIdentifier);
            if (!forceNewCode && await cacheProvider.ExistsAsync(key))
            {
                return ConfirmationCodeSendStatus.UseExistingCode;
            }
            else
            {
                await SaveAndSendCodeAsync(key, userIdentifier, sendActionAsync, logText);
                return forceNewCode ? ConfirmationCodeSendStatus.ForceNewCode : ConfirmationCodeSendStatus.NewCode;
            }
        }

        private async Task SaveAndSendCodeAsync(string key, string userIdentifier, Func<User, string, Task> sendActionAsync, string logText)
        {
            var confirmationCode = RandomGenerator.GenerateCode(Constants.Models.User.ConfirmationCodeLength).ToUpper();
            var confirmationCodeObj = new ConfirmationCode();
            await secretHashLogic.AddSecretHashAsync(confirmationCodeObj, confirmationCode);
            await cacheProvider.SetAsync(key, confirmationCodeObj.ToJson(), settings.ConfirmationCodeLifetime);

            var user = await accountLogic.GetUserAsync(userIdentifier);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled, trying to send {logText}.");
            }

            await sendActionAsync(user, confirmationCode);
            logger.ScopeTrace(() => $"{logText} send to '{userIdentifier}' for user id '{user.UserId}'.", triggerEvent: true);
        }

        private async Task<EmailContent> AddAddressAndInfoAsync(EmailContent emailContent)
        {
            if (!RouteBinding.CompanyName.IsNullOrWhiteSpace())
            {
                var track = await trackCacheLogic.GetTrackAsync(new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName });
                var aList = new List<string>
                {
                    RouteBinding.CompanyName
                };
                if (!track.AddressLine1.IsNullOrWhiteSpace())
                {
                    aList.Add(track.AddressLine1);
                }
                if (!track.AddressLine2.IsNullOrWhiteSpace())
                {
                    aList.Add(track.AddressLine2);
                }
                if (!track.PostalCode.IsNullOrWhiteSpace() && !track.City.IsNullOrWhiteSpace())
                {
                    aList.Add($"{track.PostalCode} {track.City}");
                }
                if (!track.StateRegion.IsNullOrWhiteSpace())
                {
                    aList.Add(track.StateRegion);
                }
                if (!track.Country.IsNullOrWhiteSpace())
                {
                    aList.Add(track.Country);
                }

                emailContent.Address = HttpUtility.HtmlEncode(string.Join(" - ", aList));
            }
            else if (!string.IsNullOrWhiteSpace(settings.Address?.CompanyName))
            {
                var aList = new List<string>
                {
                    settings.Address.CompanyName
                };
                if (!settings.Address.AddressLine1.IsNullOrWhiteSpace())
                {
                    aList.Add(settings.Address.AddressLine1);
                }
                if (!settings.Address.AddressLine2.IsNullOrWhiteSpace())
                {
                    aList.Add(settings.Address.AddressLine2);
                }
                if (!settings.Address.PostalCode.IsNullOrWhiteSpace() && !settings.Address.City.IsNullOrWhiteSpace())
                {
                    aList.Add($"{settings.Address.PostalCode} {settings.Address.City}");
                }
                if (!settings.Address.StateRegion.IsNullOrWhiteSpace())
                {
                    aList.Add(settings.Address.StateRegion);
                }
                if (!settings.Address.Country.IsNullOrWhiteSpace())
                {
                    aList.Add(settings.Address.Country);
                }

                emailContent.Address = HttpUtility.HtmlEncode(string.Join(" - ", aList));
                emailContent.Info = localizer["This email is send from <a href=\"{0}\">FoxIDs</a> the European identity service provided by ITfoxtec."];
            }

            return emailContent;
        }

        private async Task<User> VerifyCodeAsync(SendType sendType, string keyElement, string userIdentifier, string code, Func<User, string, Task> sendActionAsync, Func<User, Task> onSuccess, string logText)
        {
            var failingConfirmatioCount = await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, FailingLoginTypes.SmsCode);

            var key = CodeCacheKey(keyElement, userIdentifier);
            var confirmationCodeValue = await cacheProvider.GetAsync(key);
            if (!confirmationCodeValue.IsNullOrEmpty())
            {
                var confirmationCode = confirmationCodeValue.ToObject<ConfirmationCode>();
                if (await secretHashLogic.ValidateSecretAsync(confirmationCode, code.ToUpper()))
                {
                    await failingLoginLogic.ResetFailingLoginCountAsync(userIdentifier, FailingLoginTypes.SmsCode);

                    var user = await accountLogic.GetUserAsync(userIdentifier);
                    if (user == null || user.DisableAccount)
                    {
                        throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled, trying to do {logText}.");
                    }

                    switch (sendType)
                    {
                        case SendType.Sms:
                            if (!user.PhoneVerified)
                            {
                                user.PhoneVerified = true;
                                await tenantDataRepository.SaveAsync(user);
                            }
                            break;
                        case SendType.Email:
                            if (!user.EmailVerified)
                            {
                                user.EmailVerified = true;
                                await tenantDataRepository.SaveAsync(user);
                            }
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                   
                    if (onSuccess != null)
                    {
                        await onSuccess(user);
                    }

                    await cacheProvider.DeleteAsync(key);
                    logger.ScopeTrace(() => $"User '{userIdentifier}' {logText} verified for user id '{user.UserId}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                    return user;
                }
                else
                {
                    var increasedfailingConfirmationCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(userIdentifier, FailingLoginTypes.SmsCode);
                    logger.ScopeTrace(() => $"Failing count increased for user '{userIdentifier}', {logText} invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingConfirmationCount), triggerEvent: true);
                    throw new InvalidCodeException($"Invalid {logText}, user '{userIdentifier}'.");
                }
            }
            else
            {
                logger.ScopeTrace(() => $"There is not a {logText} to compare with, user '{userIdentifier}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                await SaveAndSendCodeAsync(key, userIdentifier, sendActionAsync, logText);
                throw new CodeNotExistsException($"{logText} not found.");
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

        private string CodeCacheKey(string keyElement, string userIdentifier)
        {
            return $"{keyElement}_{RouteBinding.TenantNameDotTrackName}_{userIdentifier}";
        }

        private enum SendType
        {
            Sms,
            Email
        }
    }
}