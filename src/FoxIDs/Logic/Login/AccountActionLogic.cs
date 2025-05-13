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
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public AccountActionLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ICacheProvider cacheProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, SecretHashLogic secretHashLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, SendSmsLogic sendSmsLogic, SendEmailLogic sendEmailLogic, TrackCacheLogic trackCacheLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)        {
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
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        #region ConfirmationCode
        public Task<ConfirmationCodeSendStatus> SendPhoneConfirmationCodeSmsAsync(string phone, bool forceNewCode)
        {
            phone = phone?.Trim();
            return SendCodeAsync(SmsConfirmationCodeKeyElement, phone, GetSmsSendConfirmationCodeAction(), forceNewCode, GetConfirmationCodeSmsAction(), SmsConfirmationCodeLogText);
        }

        public Task<User> VerifyPhoneConfirmationCodeSmsAsync(string phone, string code)
        {
            phone = phone?.Trim();
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
                Sms = localizer["{0} is your {1} confirmation code to verify your phone number. Don't share your code with anyone.", code, GetCompanyName()]
            };
        }

        private EmailContent GetEmailConfirmationCodeEmailContent(string code)
        {
            return new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0} verify your phone number", $"{GetCompanyName()} -"],
                Body = GetBodyHtml(localizer["This is you {0} confirmation code to verify your phone number. Don't share your code with anyone.", GetCompanyName()], localizer["Confirmation code: {0}", GetCodeHtml(code)])
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
        #endregion

        #region PasswordCode
        public Task<ConfirmationCodeSendStatus> SendPhoneSetPasswordCodeSmsAsync(string phone, bool forceNewCode)
        {
            phone = phone?.Trim();
            return SendCodeAsync(SmsSetPasswordCodeKeyElement, phone, GetSmsSendSetPasswordAction(), forceNewCode, GetConfirmationCodeSmsAction(), SmsSetPasswordCodeLogText);
        }

        public async Task<User> VerifyPhoneSetPasswordCodeSmsAndSetPasswordAsync(string phone, string code, string newPassword, bool deleteRefreshTokenGrants)
        {
            phone = phone?.Trim();
            Func<User, Task> onSuccess = (user) => accountLogic.SetPasswordUserAsync(user, newPassword);
            var user = await VerifyCodeAsync(SendType.Sms, SmsSetPasswordCodeKeyElement, phone, code, GetSmsSendSetPasswordAction(), onSuccess, GetConfirmationCodeSmsAction(), SmsSetPasswordCodeLogText);
            if (deleteRefreshTokenGrants)
            {
                await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsByPhoneAsync(phone);
            }
            return user;
        }

        public Task<ConfirmationCodeSendStatus> SendEmailSetPasswordCodeAsync(string email, bool forceNewCode)
        {
            email = email?.Trim()?.ToLower();
            return SendCodeAsync(EmailSetPasswordCodeKeyElement, email, GetEmailSendSetPasswordAction(), forceNewCode, GetConfirmationCodeEmailAction(), EmailSetPasswordCodeLogText);
        }

        public async Task<User> VerifyEmailSetPasswordCodeAndSetPasswordAsync(string email, string code, string newPassword, bool deleteRefreshTokenGrants)
        {
            email = email?.Trim()?.ToLower();
            Func<User, Task> onSuccess = (user) => accountLogic.SetPasswordUserAsync(user, newPassword);
            var user = await VerifyCodeAsync(SendType.Email, EmailSetPasswordCodeKeyElement, email, code, GetEmailSendSetPasswordAction(), onSuccess, GetConfirmationCodeEmailAction(), EmailSetPasswordCodeLogText);
            if (deleteRefreshTokenGrants)
            {
                await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsByEmailAsync(email);
            }            
            return user;
        }

        private string SmsSetPasswordCodeLogText => "Phone (SMS) set password code";
        private string SmsSetPasswordCodeKeyElement => "sms_set_password_code";

        private string EmailSetPasswordCodeLogText => "Email set password code";
        private string EmailSetPasswordCodeKeyElement => "set_password_code";

        private SmsContent GetPhoneSetPasswordCodeSms(string code)
        {
            return new SmsContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Sms = localizer["{0} is your {1} confirmation code to set your desired password. Don't share your code with anyone.", code, GetCompanyName()]
            };
        }

        private EmailContent GetEmailSetPasswordCodeEmailContent(string code)
        {
            return new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0} set password", $"{GetCompanyName()} -"],
                Body = GetBodyHtml(localizer["This is you {0} confirmation code to set your desired password. Don't share your code with anyone.", GetCompanyName()], localizer["Confirmation code: {0}", GetCodeHtml(code)])
            };
        }

        private Func<User, string, Task> GetSmsSendSetPasswordAction()
        {
            return (user, code) => sendSmsLogic.SendSmsAsync(user.Phone, GetPhoneSetPasswordCodeSms(code));
        }

        private Func<User, string, Task> GetEmailSendSetPasswordAction()
        {
            return async (user, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), user.Email), await AddAddressAndInfoAsync(GetEmailSetPasswordCodeEmailContent(code)));
        }
        #endregion

        #region TwoFactorCode
        public Task SendPhoneTwoFactorCodeSmsAsync(string phone)
        {
            phone = phone?.Trim();
            return SendCodeAsync(SmsTwoFactorCodeKeyElement, phone, GetSmsSendTwoFactorCodeAction(), true, GetTwoFactorConfirmationCodeSmsAction(), SmsTwoFactorCodeLogText);
        }

        public Task<User> VerifyPhoneTwoFactorCodeSmsAsync(string phone, string code)
        {
            phone = phone?.Trim();
            return VerifyCodeAsync(SendType.TwoFactorSms, SmsTwoFactorCodeKeyElement, phone, code, GetSmsSendTwoFactorCodeAction(), null, GetTwoFactorConfirmationCodeSmsAction(), SmsTwoFactorCodeLogText);
        }

        public Task SendEmailTwoFactorCodeAsync(string email)
        {
            email = email?.Trim()?.ToLower();
            return SendCodeAsync(EmailTwoFactorCodeKeyElement, email, GetEmailSendTwoFactorCodeAction(), true, GetTwoFactorConfirmationCodeEmailAction(), EmailTwoFactorCodeLogText);
        }

        public Task<User> VerifyEmailTwoFactorCodeAsync(string email, string code)
        {
            email = email?.ToLower();
            return VerifyCodeAsync(SendType.TwoFactorEmail, EmailTwoFactorCodeKeyElement, email, code, GetEmailSendTwoFactorCodeAction(), null, GetTwoFactorConfirmationCodeEmailAction(), EmailTwoFactorCodeLogText);
        }

        private string SmsTwoFactorCodeLogText => "Phone (SMS) two-factor code";
        private string SmsTwoFactorCodeKeyElement => "sms_two_factor_code";

        private string EmailTwoFactorCodeLogText => "Email two-factor code";
        private string EmailTwoFactorCodeKeyElement => "email_two_factor_code";

        private SmsContent GetPhoneTwoFactorCodeSms(string code)
        {
            return new SmsContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Sms = localizer["{0} is your {1} two-factor code. Don't share your code with anyone.", code, GetCompanyName()]
            };
        }

        private EmailContent GetEmailTwoFactorCodeEmailContent(string code)
        {
            return new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0} two-factor code", $"{GetCompanyName()} -"],
                Body = GetBodyHtml(localizer["This is you {0} two-factor code. Don't share your code with anyone.", GetCompanyName()], localizer["Two-factor code: {0}", GetCodeHtml(code)])
            };
        }

        private Func<User, string, Task> GetSmsSendTwoFactorCodeAction()
        {
            return (user, code) => sendSmsLogic.SendSmsAsync(user.Phone, GetPhoneTwoFactorCodeSms(code));
        }

        private Func<User, string, Task> GetEmailSendTwoFactorCodeAction()
        {
            return async (user, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), user.Email), await AddAddressAndInfoAsync(GetEmailTwoFactorCodeEmailContent(code)));
        }
        #endregion

        private string GetCompanyName()
        {
            return HttpUtility.HtmlEncode(RouteBinding.CompanyName.IsNullOrWhiteSpace() ? "FoxIDs" : RouteBinding.CompanyName);
        }

        private string GetBodyHtml(string section1, string section2)
        {
            var codeHtml = string.Format(
@"<table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
  <tbody>
    <tr>
      <td>
        {0}
      </td>
    </tr>
    <tr><td style=""height: 10px;"">&nbsp;</td></tr>
    <tr>
      <td>
        {1}
      </td>
    </tr>
</tbody>
</table>", section1, section2);
            return codeHtml;
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
                emailContent.Info = localizer["This email is send from <a href=\"{0}\">FoxIDs</a> the European Identity Service."];
            }

            return emailContent;
        }

        private async Task<User> VerifyCodeAsync(SendType sendType, string keyElement, string userIdentifier, string code, Func<User, string, Task> sendActionAsync, Func<User, Task> onSuccess, Func<string, Task<string>> confirmationCodeActionAsync, string logText)
        {
            var failingConfirmatioCount = await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, GetFailingLoginType(sendType));

            var cacheKey = CodeCacheKey(keyElement, userIdentifier);
            var confirmationCodeValue = await cacheProvider.GetAsync(cacheKey);
            if (!confirmationCodeValue.IsNullOrEmpty())
            {
                var confirmationCode = confirmationCodeValue.ToObject<ConfirmationCode>();
                if (await secretHashLogic.ValidateSecretAsync(confirmationCode, code.ToUpper()))
                {
                    await failingLoginLogic.ResetFailingLoginCountAsync(userIdentifier, GetFailingLoginType(sendType));

                    var user = await accountLogic.GetUserAsync(userIdentifier);
                    if (user == null || user.DisableAccount)
                    {
                        throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled, trying to do {logText}.");
                    }

                    switch (sendType)
                    {
                        case SendType.Sms:
                        case SendType.TwoFactorSms:
                            if (!user.PhoneVerified)
                            {
                                user.PhoneVerified = true;
                                await tenantDataRepository.SaveAsync(user);
                            }
                            break;
                        case SendType.Email:
                        case SendType.TwoFactorEmail:
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
                    var increasedfailingConfirmationCount = await failingLoginLogic.IncreaseFailingLoginCountAsync(userIdentifier, GetFailingLoginType(sendType));
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

        private FailingLoginTypes GetFailingLoginType(SendType sendType)
        {
            switch (sendType)
            {
                case SendType.Sms:
                    return FailingLoginTypes.SmsCode;
                case SendType.Email:
                    return FailingLoginTypes.EmailCode;
                case SendType.TwoFactorSms:
                    return FailingLoginTypes.TwoFactorSmsCode;
                case SendType.TwoFactorEmail:
                    return FailingLoginTypes.TwoFactorEmailCode;
                default:
                    throw new NotImplementedException();
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

        private Func<string, Task<string>> GetTwoFactorConfirmationCodeSmsAction()
        {
            return (cacheKey) => CreateAndSaveConfirmationCodeAsync(cacheKey, Constants.Models.User.ConfirmationCodeSmsLength, settings.TwoFactorCodeSmsLifetime);
        }

        private Func<string, Task<string>> GetTwoFactorConfirmationCodeEmailAction()
        {
            return (cacheKey) => CreateAndSaveConfirmationCodeAsync(cacheKey, Constants.Models.User.ConfirmationCodeEmailLength, settings.TwoFactorCodeEmailLifetime);
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
            Email,
            TwoFactorSms,
            TwoFactorEmail
        }
    }
}