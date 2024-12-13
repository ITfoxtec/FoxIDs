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
        private readonly SendEmailLogic sendEmailLogic;
        private readonly TrackCacheLogic trackCacheLogic;

        public AccountActionLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, ICacheProvider cacheProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, SecretHashLogic secretHashLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, SendEmailLogic sendEmailLogic, TrackCacheLogic trackCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
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
            this.trackCacheLogic = trackCacheLogic;
        }

        public Task<ConfirmationCodeSendStatus> SendEmailConfirmationCodeAsync(string email, bool forceNewCode)
        {
            email = email?.ToLowerInvariant();
            return SendEmailCodeAsync(GetEmailConfirmationCodeEmailContent(), EmailConfirmationCodeKeyElement, email, forceNewCode, "email");
        }

        public Task<User> VerifyEmailConfirmationCodeAsync(string email, string code)
        {
            email = email?.ToLowerInvariant();
            return VerifyEmailCodeAsync(GetEmailConfirmationCodeEmailContent(), EmailConfirmationCodeKeyElement, null, email, code, "email");
        }

        private string EmailConfirmationCodeKeyElement => "email_confirmation_code";

        private Func<string, EmailContent> GetEmailConfirmationCodeEmailContent()
        {
            return (code) => new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0}Email confirmation", $"{GetCompanyName()} - "],
                Body = localizer["Your{0}email confirmation code: {1}", $" {GetCompanyName()} ", GetCodeHtml(code)]
            };
        }

        public Task<ConfirmationCodeSendStatus> SendResetPasswordCodeAsync(string email, bool forceNewCode)
        {
            email = email?.ToLowerInvariant();
            return SendEmailCodeAsync(GetResetPasswordCodeEmailContent(), ResetPasswordCodeKeyElement, email, forceNewCode, "reset password");
        }

        public Task<User> VerifyResetPasswordCodeAndSetPasswordAsync(string email, string code, string newPassword)
        {
            email = email?.ToLowerInvariant();
            Func<User, Task> onSuccess = (user) => accountLogic.SetPasswordUser(user, newPassword);
            return VerifyEmailCodeAsync(GetResetPasswordCodeEmailContent(), ResetPasswordCodeKeyElement, onSuccess, email, code, "reset password");
        }

        private string ResetPasswordCodeKeyElement => "reset_password_code";

        private Func<string, EmailContent> GetResetPasswordCodeEmailContent()
        {
            return (code) => new EmailContent
            {
                ParentCulture = HttpContext.GetCultureParentName(),
                Subject = localizer["{0}Reset password", $"{GetCompanyName()} - "],
                Body = localizer["Your{0}reset password confirmation code: {1}", $" {GetCompanyName()} ", GetCodeHtml(code)]
            };
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

        private async Task<ConfirmationCodeSendStatus> SendEmailCodeAsync(Func<string, EmailContent> emailContent, string keyElement, string email, bool forceNewCode, string logText)
        {
            var key = EmailConfirmationCodeCacheKey(keyElement, email);
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

        private async Task SaveAndSendEmailCode(string key, string email, Func<string, EmailContent> emailContent, string logText)
        {
            var confirmationCode = RandomGenerator.GenerateCode(Constants.Models.User.ConfirmationCodeLength).ToUpper();
            var confirmationCodeObj = new ConfirmationCode();
            await secretHashLogic.AddSecretHashAsync(confirmationCodeObj, confirmationCode);
            await cacheProvider.SetAsync(key, confirmationCodeObj.ToJson(), settings.ConfirmationCodeLifetime);

            var user = await accountLogic.GetUserAsync(email);
            if (user == null || user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{email}' do not exist or is disabled, trying to send {logText} confirmation code.");
            }

            await sendEmailLogic.SendEmailAsync(new MailboxAddress(GetDisplayName(user), user.Email), await AddAddressAndInfoAsync(emailContent(confirmationCode)));

            logger.ScopeTrace(() => $"Email with {logText} confirmation code send to '{user.Email}' for user id '{user.UserId}'.", triggerEvent: true);
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
                emailContent.Info = localizer[@"This email is send from <a href=""{0}"">FoxIDs</a> the European identity service provided by <a href=""{0}"">ITfoxtec</a>."];
            }

            return emailContent;
        }

        private async Task<User> VerifyEmailCodeAsync(Func<string, EmailContent> emailContent, string keyElement, Func<User, Task> onSuccess, string email, string code, string logText)
        {
            var failingConfirmatioCount = await failingLoginLogic.VerifyFailingLoginCountAsync(email);

            var key = EmailConfirmationCodeCacheKey(keyElement, email);
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