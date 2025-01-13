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
            return SendCodeAsync(SmsConfirmationCodeKeyElement, phone, GetSmsSendConfirmationCodeAction(), forceNewCode, GetConfirmationCodeSmsAction(), SmsConfirmationCodeLogText);
        }

        public Task<User> VerifyPhoneConfirmationCodeSmsAsync(string phone, string code)
        {
            return VerifyCodeAsync(SendType.Sms, SmsConfirmationCodeKeyElement, phone, code, GetSmsSendConfirmationCodeAction(), null, GetConfirmationCodeSmsAction(), SmsConfirmationCodeLogText);
        }

        public Task<ConfirmationCodeSendStatus> SendEmailConfirmationCodeAsync(string email, bool forceNewCode)
        {
            email = email?.Trim()?.ToLower();
            return SendCodeAsync(EmailConfirmationCodeKeyElement, email, GetEmailSendConfirmationCodeAction(), forceNewCode, GetConfirmationCodeEmailAction(), EmailConfirmationCodeLogText);
        }

        public Task<User> VerifyEmailConfirmationCodeAsync(string email, string code)
        {
            email = email?.ToLower();
            return VerifyCodeAsync(SendType.Email, EmailConfirmationCodeKeyElement, email, code, GetEmailSendConfirmationCodeAction(), null, GetConfirmationCodeEmailAction(), EmailConfirmationCodeLogText);
        }

        private string SmsConfirmationCodeLogText => "Phone (SMS) confirmation code";
        private string SmsConfirmationCodeKeyElement => "sms_confirmation_code";

        private string EmailConfirmationCodeLogText => "Email confirmation code";
        private string EmailConfirmationCodeKeyElement => "email_confirmation_code";

        private SmsContent GetPhoneConfirmationCodeSms(string code)
        {
            return new SmsContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Sms = localizer["Your{0}SMS confirmation code: {1}", $" {GetCompanyName()} ", code]
            };
        }

        private EmailContent GetEmailConfirmationCodeEmailContent(string code)
        {
            return new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0}Email confirmation", $"{GetCompanyName()} - "],
                Body = localizer["Your{0}email confirmation code: {1}", $" {GetCompanyName()} ", GetCodeHtml(code)]
            };
        }

        private Func<User, string, Task> GetSmsSendConfirmationCodeAction()
        {
            return (user, code) => sendSmsLogic.SendSmsAsync(user.Phone, GetPhoneConfirmationCodeSms(code));
        }

        private Func<User, string, Task> GetEmailSendConfirmationCodeAction()
        {
            return async (user, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), user.Email), await AddAddressAndInfoAsync(GetEmailConfirmationCodeEmailContent(code)));
        }

        public Task<ConfirmationCodeSendStatus> SendPhoneResetPasswordCodeSmsAsync(string phone, bool forceNewCode)
        {
            phone = phone?.Trim();
            return SendCodeAsync(SmsResetPasswordCodeKeyElement, phone, GetSmsSendResetPasswordAction(), forceNewCode, GetConfirmationCodeSmsAction(), SmsResetPasswordCodeLogText);
        }

        public Task<User> VerifyPhoneResetPasswordCodeSmsAndSetPasswordAsync(string phone, string code, string newPassword)
        {
            phone = phone?.Trim();
            Func<User, Task> onSuccess = (user) => accountLogic.SetPasswordUser(user, newPassword);
            return VerifyCodeAsync(SendType.Sms, SmsResetPasswordCodeKeyElement, phone, code, GetSmsSendResetPasswordAction(), onSuccess, GetConfirmationCodeSmsAction(), SmsResetPasswordCodeLogText);
        }

        public Task<ConfirmationCodeSendStatus> SendEmailResetPasswordCodeAsync(string email, bool forceNewCode)
        {
            email = email?.Trim()?.ToLower();
            return SendCodeAsync(EmailResetPasswordCodeKeyElement, email, GetEmailSendResetPasswordAction(), forceNewCode, GetConfirmationCodeEmailAction(), EmailResetPasswordCodeLogText);
        }

        public Task<User> VerifyEmailResetPasswordCodeAndSetPasswordAsync(string email, string code, string newPassword)
        {
            email = email?.Trim()?.ToLower();
            Func<User, Task> onSuccess = (user) => accountLogic.SetPasswordUser(user, newPassword);
            return VerifyCodeAsync(SendType.Email, EmailResetPasswordCodeKeyElement, email, code, GetEmailSendResetPasswordAction(), onSuccess, GetConfirmationCodeEmailAction(), EmailResetPasswordCodeLogText);
        }

        private string SmsResetPasswordCodeLogText => "Phone (SMS) reset password code";
        private string SmsResetPasswordCodeKeyElement => "sms_reset_password_code";

        private string EmailResetPasswordCodeLogText => "Email reset password code";
        private string EmailResetPasswordCodeKeyElement => "reset_password_code";

        private SmsContent GetPhoneResetPasswordCodeSms(string code)
        {
            return new SmsContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Sms = localizer["Your{0}reset password confirmation code: {1}", $" {GetCompanyName()} ", code]
            };
        }

        private EmailContent GetEmailResetPasswordCodeEmailContent(string code)
        {
            return new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0}Reset password", $"{GetCompanyName()} - "],
                Body = localizer["Your{0}reset password confirmation code: {1}", $" {GetCompanyName()} ", GetCodeHtml(code)]
            };
        }

        private Func<User, string, Task> GetSmsSendResetPasswordAction()
        {
            return (user, code) => sendSmsLogic.SendSmsAsync(user.Phone, GetPhoneResetPasswordCodeSms(code));
        }

        private Func<User, string, Task> GetEmailSendResetPasswordAction()
        {
            return async (user, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), user.Email), await AddAddressAndInfoAsync(GetEmailResetPasswordCodeEmailContent(code)));
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

        private async Task<ConfirmationCodeSendStatus> SendCodeAsync(string keyElement, string userIdentifier, Func<User, string, Task> sendActionAsync, bool forceNewCode, Func<string, Task<string>> confirmationCodeActionAsync, string logText)
        {
            var cacheKey = CodeCacheKey(keyElement, userIdentifier);
            if (!forceNewCode && await cacheProvider.ExistsAsync(cacheKey))
            {
                return ConfirmationCodeSendStatus.UseExistingCode;
            }
            else
            {
                await SaveAndSendCodeAsync(cacheKey, userIdentifier, sendActionAsync, confirmationCodeActionAsync, logText);
                return forceNewCode ? ConfirmationCodeSendStatus.ForceNewCode : ConfirmationCodeSendStatus.NewCode;
            }
        }

        private async Task SaveAndSendCodeAsync(string cacheKey, string userIdentifier, Func<User, string, Task> sendActionAsync, Func<string, Task<string>> confirmationCodeActionAsync, string logText)
        {
            var user = await accountLogic.GetUserAsync(userIdentifier);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled, trying to send {logText}.");
            }

            await sendActionAsync(user, await confirmationCodeActionAsync(cacheKey));
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

        private async Task<User> VerifyCodeAsync(SendType sendType, string keyElement, string userIdentifier, string code, Func<User, string, Task> sendActionAsync, Func<User, Task> onSuccess, Func<string, Task<string>> confirmationCodeActionAsync, string logText)
        {
            var failingConfirmatioCount = await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, sendType == SendType.Sms ? FailingLoginTypes.SmsCode : FailingLoginTypes.EmailCode);

            var cacheKey = CodeCacheKey(keyElement, userIdentifier);
            var confirmationCodeValue = await cacheProvider.GetAsync(cacheKey);
            if (!confirmationCodeValue.IsNullOrEmpty())
            {
                var confirmationCode = confirmationCodeValue.ToObject<ConfirmationCode>();
                if (await secretHashLogic.ValidateSecretAsync(confirmationCode, code.ToUpper()))
                {
                    await failingLoginLogic.ResetFailingLoginCountAsync(userIdentifier, sendType == SendType.Sms ? FailingLoginTypes.SmsCode : FailingLoginTypes.EmailCode);

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

                    await cacheProvider.DeleteAsync(cacheKey);
                    logger.ScopeTrace(() => $"User '{userIdentifier}' {logText} verified for user id '{user.UserId}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                    return user;
                }
                else
                {
                    var increasedfailingConfirmationCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(userIdentifier, sendType == SendType.Sms ? FailingLoginTypes.SmsCode : FailingLoginTypes.EmailCode);
                    logger.ScopeTrace(() => $"Failing count increased for user '{userIdentifier}', {logText} invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingConfirmationCount), triggerEvent: true);
                    throw new InvalidCodeException($"Invalid {logText}, user '{userIdentifier}'.");
                }
            }
            else
            {
                logger.ScopeTrace(() => $"There is not a {logText} to compare with, user '{userIdentifier}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                await SaveAndSendCodeAsync(cacheKey, userIdentifier, sendActionAsync, confirmationCodeActionAsync, logText);
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

        private Func<string, Task<string>> GetConfirmationCodeSmsAction()
        {
            return (cacheKey) => CreateAndSaveConfirmationCodeAsync(cacheKey, Constants.Models.User.ConfirmationCodeSmsLength, settings.ConfirmationCodeSmsLifetime);
        }

        private Func<string, Task<string>> GetConfirmationCodeEmailAction()
        {
            return (cacheKey) => CreateAndSaveConfirmationCodeAsync(cacheKey, Constants.Models.User.ConfirmationCodeEmailLength, settings.ConfirmationCodeEmailLifetime);
        }

        private async Task<string> CreateAndSaveConfirmationCodeAsync(string cacheKey, int confirmationCodeLength, int confirmationCodeLifetime)
        {
            var confirmationCode = RandomGenerator.GenerateCode(confirmationCodeLength).ToUpper();
            var confirmationCodeObj = new ConfirmationCode();
            await secretHashLogic.AddSecretHashAsync(confirmationCodeObj, confirmationCode);
            await cacheProvider.SetAsync(cacheKey, confirmationCodeObj.ToJson(), confirmationCodeLifetime);
            return confirmationCode;
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