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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace FoxIDs.Logic
{
    public class AccountActionLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        protected readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ICacheProvider cacheProvider;
        private readonly IStringLocalizer localizer;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SecretHashLogic secretHashLogic;
        private readonly FailingLoginLogic failingLoginLogic;
        private readonly SendSmsLogic sendSmsLogic;
        private readonly SendEmailLogic sendEmailLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;
        private readonly ActiveSessionLogic activeSessionLogic;

        public AccountActionLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, ICacheProvider cacheProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, SecretHashLogic secretHashLogic, FailingLoginLogic failingLoginLogic, SendSmsLogic sendSmsLogic, SendEmailLogic sendEmailLogic, PlanUsageLogic planUsageLogic, TrackCacheLogic trackCacheLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic, ActiveSessionLogic activeSessionLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.cacheProvider = cacheProvider;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.secretHashLogic = secretHashLogic;
            this.failingLoginLogic = failingLoginLogic;
            this.sendSmsLogic = sendSmsLogic;
            this.sendEmailLogic = sendEmailLogic;
            this.planUsageLogic = planUsageLogic;
            this.trackCacheLogic = trackCacheLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
            this.activeSessionLogic = activeSessionLogic;
        }

        #region PasswordlessCode
        public async Task SendPhonePasswordlessCodeSmsAsync(string userIdentifier)
        {
            userIdentifier = userIdentifier?.Trim();
            await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, FailingLoginTypes.InternalLogin, sendingCode: true);
            (_, var user) = await SendCodeAsync(SendType.PasswordlessSms, SmsPasswordlessCodeKeyElement, userIdentifier, GetSmsSendPasswordlessCodeAction(), true, GetConfirmationCodeSmsAction(), SmsPasswordlessCodeLogText);
            await planUsageLogic.LogPasswordlessSmsEventAsync(user?.Phone ?? userIdentifier);
            return;
        }

        public Task<User> VerifyPhonePasswordlessCodeSmsAsync(string userIdentifier, string code)
        {
            userIdentifier = userIdentifier?.Trim();
            return VerifyCodeAsync(SendType.PasswordlessSms, SmsPasswordlessCodeKeyElement, userIdentifier, code, GetSmsSendPasswordlessCodeAction(), null, GetConfirmationCodeSmsAction(), SmsPasswordlessCodeLogText);
        }

        public async Task SendEmailPasswordlessCodeAsync(string userIdentifier)
        {
            userIdentifier = userIdentifier?.Trim()?.ToLower();
            await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, FailingLoginTypes.InternalLogin, sendingCode: true);
            _ = await SendCodeAsync(SendType.PasswordlessEmail, EmailPasswordlessCodeKeyElement, userIdentifier, GetEmailSendPasswordlessCodeAction(), true, GetConfirmationCodeEmailAction(), EmailPasswordlessCodeLogText);
            planUsageLogic.LogPasswordlessEmailEvent();
            return;
        }

        public Task<User> VerifyEmailPasswordlessCodeAsync(string userIdentifier, string code)
        {
            userIdentifier = userIdentifier?.ToLower();
            return VerifyCodeAsync(SendType.PasswordlessEmail, EmailPasswordlessCodeKeyElement, userIdentifier, code, GetEmailSendPasswordlessCodeAction(), null, GetConfirmationCodeEmailAction(), EmailPasswordlessCodeLogText);
        }

        private string SmsPasswordlessCodeLogText => "Passwordless one-time password via SMS";
        private string SmsPasswordlessCodeKeyElement => "sms_passwordless_code";

        private string EmailPasswordlessCodeLogText => "Passwordless one-time password via email";
        private string EmailPasswordlessCodeKeyElement => "email_passwordless_code";

        private SmsContent GetPhonePasswordlessCodeSms(string code)
        {
            return new SmsContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Sms = localizer["{0} is your {1} one-time password for logging in. Don't share your one-time password with anyone.", code, GetCompanyName()]
            };
        }

        private EmailContent GetEmailPasswordlessCodeEmailContent(string code)
        {
            return new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0} one-time password", $"{GetCompanyName()} -"],
                Body = GetBodyHtml(localizer["This is you {0} one-time password for logging in. Don't share your one-time password with anyone.", GetCompanyName()], localizer["One-time password: {0}", GetCodeHtml(code)])
            };
        }

        private Func<User, string, string, Task> GetSmsSendPasswordlessCodeAction()
        {
            return (user, phone, code) => sendSmsLogic.SendSmsAsync(phone, GetPhonePasswordlessCodeSms(code));
        }

        private Func<User, string, string, Task> GetEmailSendPasswordlessCodeAction()
        {
            return async (user, email, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), email), await AddAddressAndInfoAsync(GetEmailPasswordlessCodeEmailContent(code)));
        }
        #endregion

        #region ConfirmationCode
        public async Task<ConfirmationCodeSendStatus> SendPhoneConfirmationCodeSmsAsync(string phone, bool forceNewCode)
        {
            phone = phone?.Trim();
            await failingLoginLogic.VerifyFailingLoginCountAsync(phone, FailingLoginTypes.SmsCode);
            (var sendStatus, var user) = await SendCodeAsync(SendType.Sms, SmsConfirmationCodeKeyElement, phone, GetSmsSendConfirmationCodeAction(), forceNewCode, GetConfirmationCodeSmsAction(), SmsConfirmationCodeLogText);
            if (sendStatus != ConfirmationCodeSendStatus.UseExistingCode)
            {
                await planUsageLogic.LogConfirmationSmsEventAsync(user?.Phone ?? phone);
            }
            return sendStatus;
        }

        public Task<User> VerifyPhoneConfirmationCodeSmsAsync(string phone, string code)
        {
            phone = phone?.Trim();
            return VerifyCodeAsync(SendType.Sms, SmsConfirmationCodeKeyElement, phone, code, GetSmsSendConfirmationCodeAction(), null, GetConfirmationCodeSmsAction(), SmsConfirmationCodeLogText);
        }

        public async Task<ConfirmationCodeSendStatus> SendEmailConfirmationCodeAsync(string email, bool forceNewCode)
        {
            email = email?.Trim()?.ToLower();
            await failingLoginLogic.VerifyFailingLoginCountAsync(email, FailingLoginTypes.EmailCode);
            (var sendStatus, _) = await SendCodeAsync(SendType.Email, EmailConfirmationCodeKeyElement, email, GetEmailSendConfirmationCodeAction(), forceNewCode, GetConfirmationCodeEmailAction(), EmailConfirmationCodeLogText);
            if (sendStatus != ConfirmationCodeSendStatus.UseExistingCode)
            {
                planUsageLogic.LogConfirmationEmailEvent();
            }
            return sendStatus;
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

        private Func<User, string, string, Task> GetSmsSendConfirmationCodeAction()
        {
            return (user, phone, code) => sendSmsLogic.SendSmsAsync(phone, GetPhoneConfirmationCodeSms(code));
        }

        private Func<User, string, string, Task> GetEmailSendConfirmationCodeAction()
        {
            return async (user, email, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), email), await AddAddressAndInfoAsync(GetEmailConfirmationCodeEmailContent(code)));
        }
        #endregion

        #region PasswordCode
        public async Task<ConfirmationCodeSendStatus> SendPhoneSetPasswordCodeSmsAsync(string userIdentifier, string phone, bool forceNewCode)
        {
            phone = phone?.Trim();
            await failingLoginLogic.VerifyFailingLoginCountAsync(phone, FailingLoginTypes.SmsCode);
            (var sendStatus, var user) = await SendCodeAsync(SendType.SetPasswordSms, SmsSetPasswordCodeKeyElement, userIdentifier, GetSmsSendSetPasswordAction(), forceNewCode, GetConfirmationCodeSmsAction(), SmsSetPasswordCodeLogText, sendIdentifier: phone);
            if (sendStatus != ConfirmationCodeSendStatus.UseExistingCode)
            {
                await planUsageLogic.LogSetPasswordSmsEventAsync(user?.Phone ?? phone);
            }
            return sendStatus;
        }

        public async Task<User> VerifyPhoneSetPasswordCodeSmsAndSetPasswordAsync(string userIdentifier, string phone, string code, string newPassword, bool deleteRefreshTokenGrants, bool deleteActiveSessions)
        {
            phone = phone?.Trim();
            Func<User, Task> onSuccess = (user) => GetAccountLogic().SetPasswordUserAsync(user, newPassword);
            var user = await VerifyCodeAsync(SendType.SetPasswordSms, SmsSetPasswordCodeKeyElement, userIdentifier, code, GetSmsSendSetPasswordAction(), onSuccess, GetConfirmationCodeSmsAction(), SmsSetPasswordCodeLogText, sendIdentifier: phone);
            if (deleteRefreshTokenGrants)
            {
                await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsByPhoneAsync(phone);
            }
            if (deleteActiveSessions)
            {
                await activeSessionLogic.DeleteSessionsByPhoneAsync(phone);
            }
            return user;
        }

        public async Task<ConfirmationCodeSendStatus> SendEmailSetPasswordCodeAsync(string userIdentifier, string email, bool forceNewCode)
        {
            email = email?.Trim()?.ToLower();
            await failingLoginLogic.VerifyFailingLoginCountAsync(email, FailingLoginTypes.EmailCode);
            (var sendStatus, _) = await SendCodeAsync(SendType.SetPasswordEmail, EmailSetPasswordCodeKeyElement, userIdentifier, GetEmailSendSetPasswordAction(), forceNewCode, GetConfirmationCodeEmailAction(), EmailSetPasswordCodeLogText, sendIdentifier: email);
            if (sendStatus != ConfirmationCodeSendStatus.UseExistingCode)
            {
                planUsageLogic.LogSetPasswordEmailEvent();
            }
            return sendStatus;
        }

        public async Task<User> VerifyEmailSetPasswordCodeAndSetPasswordAsync(string userIdentifier, string email, string code, string newPassword, bool deleteRefreshTokenGrants, bool deleteActiveSessions)
        {
            email = email?.Trim()?.ToLower();
            Func<User, Task> onSuccess = (user) => GetAccountLogic().SetPasswordUserAsync(user, newPassword);
            var user = await VerifyCodeAsync(SendType.SetPasswordEmail, EmailSetPasswordCodeKeyElement, userIdentifier, code, GetEmailSendSetPasswordAction(), onSuccess, GetConfirmationCodeEmailAction(), EmailSetPasswordCodeLogText, sendIdentifier: email);
            if (deleteRefreshTokenGrants)
            {
                await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsByEmailAsync(email);
            }            
            if (deleteActiveSessions)
            {
                await activeSessionLogic.DeleteSessionsByEmailAsync(email);
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

        private Func<User, string, string, Task> GetSmsSendSetPasswordAction()
        {
            return (user, phone, code) => sendSmsLogic.SendSmsAsync(phone, GetPhoneSetPasswordCodeSms(code));
        }

        private Func<User, string, string, Task> GetEmailSendSetPasswordAction()
        {
            return async (user, email, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), email), await AddAddressAndInfoAsync(GetEmailSetPasswordCodeEmailContent(code)));
        }
        #endregion

        #region TwoFactorCode
        public async Task SendPhoneTwoFactorCodeSmsAsync(string userIdentifier, string phone)
        {
            phone = phone?.Trim();
            await failingLoginLogic.VerifyFailingLoginCountAsync(phone, FailingLoginTypes.TwoFactorSmsCode);
            (_, var user) = await SendCodeAsync(SendType.TwoFactorSms, SmsTwoFactorCodeKeyElement, userIdentifier, GetSmsSendTwoFactorCodeAction(), true, GetTwoFactorConfirmationCodeSmsAction(), SmsTwoFactorCodeLogText, sendIdentifier: phone);
            await planUsageLogic.LogMfaSmsEventAsync(phone);
            return;
        }

        public Task<User> VerifyPhoneTwoFactorCodeSmsAsync(string userIdentifier, string phone, string code)
        {
            phone = phone?.Trim();
            return VerifyCodeAsync(SendType.TwoFactorSms, SmsTwoFactorCodeKeyElement, userIdentifier, code, GetSmsSendTwoFactorCodeAction(), null, GetTwoFactorConfirmationCodeSmsAction(), SmsTwoFactorCodeLogText, sendIdentifier: phone);
        }

        public async Task SendEmailTwoFactorCodeAsync(string userIdentifier, string email)
        {
            email = email?.Trim()?.ToLower();
            await failingLoginLogic.VerifyFailingLoginCountAsync(email, FailingLoginTypes.TwoFactorEmailCode);
            _ = await SendCodeAsync(SendType.TwoFactorEmail, EmailTwoFactorCodeKeyElement, userIdentifier, GetEmailSendTwoFactorCodeAction(), true, GetTwoFactorConfirmationCodeEmailAction(), EmailTwoFactorCodeLogText, sendIdentifier: email);
            planUsageLogic.LogMfaEmailEvent();
            return;
        }

        public Task<User> VerifyEmailTwoFactorCodeAsync(string userIdentifier, string email, string code)
        {
            email = email?.ToLower();
            return VerifyCodeAsync(SendType.TwoFactorEmail, EmailTwoFactorCodeKeyElement, userIdentifier, code, GetEmailSendTwoFactorCodeAction(), null, GetTwoFactorConfirmationCodeEmailAction(), EmailTwoFactorCodeLogText, sendIdentifier: email);
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

        private Func<User, string, string, Task> GetSmsSendTwoFactorCodeAction()
        {
            return (user, phone, code) => sendSmsLogic.SendSmsAsync(phone, GetPhoneTwoFactorCodeSms(code));
        }

        private Func<User, string, string, Task> GetEmailSendTwoFactorCodeAction()
        {
            return async (user, email, code) => await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), email), await AddAddressAndInfoAsync(GetEmailTwoFactorCodeEmailContent(code)));
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

        private async Task<(ConfirmationCodeSendStatus, User)> SendCodeAsync(SendType sendType, string keyElement, string userIdentifier, Func<User, string, string, Task> sendActionAsync, bool forceNewCode, Func<string, Task<string>> confirmationCodeActionAsync, string logText, string sendIdentifier = null)
        {
            var cacheKey = CodeCacheKey(keyElement, userIdentifier);
            if (!forceNewCode && await cacheProvider.ExistsAsync(cacheKey))
            {
                return (ConfirmationCodeSendStatus.UseExistingCode, null);
            }
            else
            {
                var user = await SaveAndSendCodeAsync(sendType, cacheKey, userIdentifier, sendIdentifier, sendActionAsync, confirmationCodeActionAsync, logText);
                return (forceNewCode ? ConfirmationCodeSendStatus.ForceNewCode : ConfirmationCodeSendStatus.NewCode, user);
            }
        }

        private async Task<User> SaveAndSendCodeAsync(SendType sendType, string cacheKey, string userIdentifier, string sendIdentifier, Func<User, string, string, Task> sendActionAsync, Func<string, Task<string>> confirmationCodeActionAsync, string logText)
        {
            var increasedfailingConfirmationCount = await failingLoginLogic.IncreaseFailingLoginOrSendingCountAsync(userIdentifier, GetFailingLoginType(sendType));

            try
            {
                var user = await GetAccountLogic().GetUserAsync(userIdentifier);
                if (user == null)
                {
                    throw new UserNotExistsException($"User '{userIdentifier}' do not exist, trying to send {logText}.");
                }
                if (user.DisableAccount)
                {
                    throw new UserNotExistsException($"User '{userIdentifier}' is disabled, trying to send {logText}.");
                }
                if (sendType == SendType.SetPasswordEmail && user.DisableSetPasswordEmail)
                {
                    throw new UserNotExistsException($"User '{userIdentifier}' has disabled set password with email, trying to send {logText}.");
                }
                if (sendType == SendType.SetPasswordSms && user.DisableSetPasswordSms)
                {
                    throw new UserNotExistsException($"User '{userIdentifier}' has disabled set password with SMS, trying to send {logText}.");
                }

                switch (sendType)
                {
                    case SendType.PasswordlessSms:
                    case SendType.Sms:
                    case SendType.SetPasswordSms:
                    case SendType.TwoFactorSms:
                        if (user.Phone.IsNullOrWhiteSpace() && sendIdentifier.IsNullOrWhiteSpace())
                        {
                            if (user.Phone.IsNullOrWhiteSpace())
                            {
                                throw new UserNotExistsException($"User '{userIdentifier}' do not have a phone number, trying to send {logText}.");
                            }
                            else
                            {
                                throw new UserNotExistsException($"User '{userIdentifier}' do not have a phone number identifier or claim, trying to send {logText}.");
                            }
                        }
                        if (sendIdentifier.IsNullOrWhiteSpace())
                        {
                            sendIdentifier = user.Phone;
                        }
                        break;
                    case SendType.PasswordlessEmail:
                    case SendType.Email:
                    case SendType.SetPasswordEmail:
                    case SendType.TwoFactorEmail:
                        if (user.Email.IsNullOrWhiteSpace() && sendIdentifier.IsNullOrWhiteSpace())
                        {
                            if (user.Email.IsNullOrWhiteSpace())
                            {
                                throw new UserNotExistsException($"User '{userIdentifier}' do not have a email, trying to send {logText}.");
                            }
                            else
                            {
                                throw new UserNotExistsException($"User '{userIdentifier}' do not have a email identifier or claim, trying to send {logText}.");
                            }
                        }
                        if (sendIdentifier.IsNullOrWhiteSpace())
                        {
                            sendIdentifier = user.Email;
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                await sendActionAsync(user, sendIdentifier, await confirmationCodeActionAsync(cacheKey));
                logger.ScopeTrace(() => $"{logText} send to '{userIdentifier}' for user id '{user.UserId}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingConfirmationCount), triggerEvent: true);
                return user;
            }
            catch
            {
                logger.ScopeTrace(() => $"{logText} NOT send to '{userIdentifier}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingConfirmationCount), triggerEvent: true);
                throw;
            }
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
                emailContent.Info = localizer["This email was sent from <a href=\"{0}\">FoxIDs</a> - your European security solution."];
            }

            return emailContent;
        }

        private async Task<User> VerifyCodeAsync(SendType sendType, string keyElement, string userIdentifier, string code, Func<User, string, string, Task> sendActionAsync, Func<User, Task> onSuccess, Func<string, Task<string>> confirmationCodeActionAsync, string logText, string sendIdentifier = null)
        {
            var failingLoginType = GetFailingLoginType(sendType);
            var failingConfirmatioCount = await failingLoginLogic.VerifyFailingLoginCountAsync(userIdentifier, failingLoginType, sendingCode: failingLoginType == FailingLoginTypes.InternalLogin);

            var cacheKey = CodeCacheKey(keyElement, userIdentifier);
            var confirmationCodeValue = await cacheProvider.GetAsync(cacheKey);
            if (!confirmationCodeValue.IsNullOrEmpty())
            {
                var confirmationCode = confirmationCodeValue.ToObject<ConfirmationCode>();
                if (await secretHashLogic.ValidateSecretAsync(confirmationCode, code.ToUpper()))
                {
                    await failingLoginLogic.ResetFailingLoginCountAsync(userIdentifier, failingLoginType);

                    var user = await GetAccountLogic().GetUserAsync(userIdentifier);
                    if (user == null || user.DisableAccount)
                    {
                        throw new UserNotExistsException($"User '{userIdentifier}' do not exist or is disabled, trying to do {logText}.");
                    }

                    switch (sendType)
                    {
                        case SendType.PasswordlessSms:
                        case SendType.Sms:
                        case SendType.SetPasswordSms:
                        case SendType.TwoFactorSms:
                            var phoneClaim = user.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.PhoneNumber);
                            if (!user.Phone.IsNullOrEmpty() && !user.PhoneVerified)
                            {
                                user.PhoneVerified = true;
                                await tenantDataRepository.SaveAsync(user);
                            }
                            else
                            {
                                var phoneVerifiedClaim = user.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.PhoneNumberVerified);
                                if (!phoneClaim.IsNullOrWhiteSpace() && (phoneVerifiedClaim.IsNullOrWhiteSpace() || phoneVerifiedClaim.Equals("false", StringComparison.OrdinalIgnoreCase) || phoneVerifiedClaim == "0"))
                                {
                                    if (phoneVerifiedClaim.IsNullOrWhiteSpace())
                                    {
                                        user.Claims.Add(new ClaimAndValues
                                        {
                                            Claim = JwtClaimTypes.PhoneNumberVerified,
                                            Values = ["true"]
                                        });
                                    }
                                    else
                                    {
                                        var pvc = user.Claims.First(c => c.Claim == JwtClaimTypes.PhoneNumberVerified);
                                        pvc.Values = ["true"];
                                    }
                                    await tenantDataRepository.SaveAsync(user);
                                }
                            }
                            if (sendIdentifier.IsNullOrWhiteSpace())
                            {
                                sendIdentifier = user.Phone ?? phoneClaim;
                            }
                            break;
                        case SendType.PasswordlessEmail:
                        case SendType.Email:
                        case SendType.SetPasswordEmail:
                        case SendType.TwoFactorEmail:
                            var emailClaim = user.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.Email);
                            if (!user.Email.IsNullOrEmpty() && !user.EmailVerified)
                            {
                                user.EmailVerified = true;
                                await tenantDataRepository.SaveAsync(user);
                            }
                            else
                            {
                                var emailVerifiedClaim = user.Claims.FindFirstOrDefaultValue(c => c.Claim == JwtClaimTypes.EmailVerified);
                                if (!emailClaim.IsNullOrWhiteSpace() && (emailVerifiedClaim.IsNullOrWhiteSpace() || emailVerifiedClaim.Equals("false", StringComparison.OrdinalIgnoreCase) || emailVerifiedClaim == "0"))
                                {
                                    if (emailVerifiedClaim.IsNullOrWhiteSpace())
                                    {
                                        user.Claims.Add(new ClaimAndValues
                                        {
                                            Claim = JwtClaimTypes.EmailVerified,
                                            Values = ["true"]
                                        });
                                    }
                                    else
                                    {
                                        var evc = user.Claims.First(c => c.Claim == JwtClaimTypes.EmailVerified);
                                        evc.Values = ["true"];
                                    }
                                    await tenantDataRepository.SaveAsync(user);
                                }
                            }
                            if (sendIdentifier.IsNullOrWhiteSpace())
                            {
                                sendIdentifier = user.Email ?? emailClaim;
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
                    var increasedfailingConfirmationCount = await failingLoginLogic.IncreaseFailingLoginOrSendingCountAsync(userIdentifier, failingLoginType);
                    logger.ScopeTrace(() => $"Failing count increased for user '{userIdentifier}', {logText} invalid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(increasedfailingConfirmationCount), triggerEvent: true);
                    throw new InvalidCodeException($"Invalid {logText}, user '{userIdentifier}'.");
                }
            }
            else
            {
                logger.ScopeTrace(() => $"There is not a {logText} to compare with, user '{userIdentifier}'.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingConfirmatioCount), triggerEvent: true);
                await SaveAndSendCodeAsync(sendType, cacheKey, userIdentifier, sendIdentifier, sendActionAsync, confirmationCodeActionAsync, logText);
                throw new CodeNotExistsException($"{logText} not found.");
            }
        }

        private FailingLoginTypes GetFailingLoginType(SendType sendType)
        {
            switch (sendType)
            {
                case SendType.PasswordlessSms:
                case SendType.PasswordlessEmail:
                    return FailingLoginTypes.InternalLogin;
                case SendType.Sms:
                case SendType.SetPasswordSms:
                    return FailingLoginTypes.SmsCode;
                case SendType.Email:
                case SendType.SetPasswordEmail:
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

        private AccountLogic GetAccountLogic() => serviceProvider.GetService<AccountLogic>();

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
            PasswordlessSms,
            PasswordlessEmail,
            Sms,
            Email,
            SetPasswordSms,
            SetPasswordEmail,
            TwoFactorSms,
            TwoFactorEmail
        }
    }
}