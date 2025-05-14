using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class BaseAccountLogic : LogicBase
    {
        protected readonly TelemetryScopedLogger logger;
        protected readonly ITenantDataRepository tenantDataRepository;
        protected readonly IMasterDataRepository masterDataRepository;
        protected readonly SecretHashLogic secretHashLogic;

        public BaseAccountLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IMasterDataRepository masterDataRepository, SecretHashLogic secretHashLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.masterDataRepository = masterDataRepository;
            this.secretHashLogic = secretHashLogic;
        }

        public async Task<User> CreateUserAsync(CreateUserObj createUserObj, bool checkUserAndPasswordPolicy = true, string tenantName = null, string trackName = null)
        {
            createUserObj.UserIdentifier.Email = createUserObj.UserIdentifier.Email?.Trim().ToLower();
            createUserObj.UserIdentifier.Email = !createUserObj.UserIdentifier.Email.IsNullOrEmpty() ? createUserObj.UserIdentifier.Email : null;
            createUserObj.UserIdentifier.Phone = createUserObj.UserIdentifier.Phone?.Trim();
            createUserObj.UserIdentifier.Phone = !createUserObj.UserIdentifier.Phone.IsNullOrEmpty() ? createUserObj.UserIdentifier.Phone : null;
            createUserObj.UserIdentifier.Username = createUserObj.UserIdentifier.Username?.Trim()?.ToLower();
            createUserObj.UserIdentifier.Username = !createUserObj.UserIdentifier.Username.IsNullOrEmpty() ? createUserObj.UserIdentifier.Username : null;
            logger.ScopeTrace(() => $"Creating user '{createUserObj.UserIdentifier.ToJson()}', Route '{RouteBinding?.Route}'.");

            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Email = createUserObj.UserIdentifier.Email,
                Phone = createUserObj.UserIdentifier.Phone,
                Username = createUserObj.UserIdentifier.Username,
                PasswordlessSms = createUserObj.PasswordlessSms,
                PasswordlessEmail = createUserObj.PasswordlessEmail,
                ConfirmAccount = createUserObj.ConfirmAccount,
                SetPasswordSms = createUserObj.SetPasswordSms,
                SetPasswordEmail = createUserObj.SetPasswordEmail,
                EmailVerified = createUserObj.EmailVerified,
                PhoneVerified = createUserObj.PhoneVerified,
                DisableAccount = createUserObj.DisableAccount,
                RequireMultiFactor = createUserObj.RequireMultiFactor,
                DisableTwoFactorApp = createUserObj.DisableTwoFactorApp,
                DisableTwoFactorSms = createUserObj.DisableTwoFactorSms,
                DisableTwoFactorEmail = createUserObj.DisableTwoFactorEmail
            };

            tenantName = tenantName ?? RouteBinding.TenantName;
            trackName = trackName ?? RouteBinding.TrackName;
            await SetIdsAsync(user, tenantName, trackName);

            var requerePassword = !(createUserObj.PasswordlessEmail || createUserObj.PasswordlessSms || createUserObj.SetPasswordEmail || createUserObj.SetPasswordSms);
            if (requerePassword)
            {
                await secretHashLogic.AddSecretHashAsync(user, createUserObj.Password);
            }
            
            if (createUserObj.Claims?.Count() > 0)
            {
                var userIdentifierClaimTypes = new List<string>();
                if (!createUserObj.UserIdentifier.Email.IsNullOrEmpty())
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.Email);
                }
                if (!createUserObj.UserIdentifier.Phone.IsNullOrEmpty())
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.PhoneNumber);
                }
                if (!createUserObj.UserIdentifier.Username.IsNullOrEmpty())
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.PreferredUsername);
                }
                createUserObj.Claims = createUserObj.Claims.Where(c => !userIdentifierClaimTypes.Where(t => t == c.Type).Any()).ToList();
                user.Claims = createUserObj.Claims.ToClaimAndValues();
            }

            if (checkUserAndPasswordPolicy)
            {
                if (await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, Email = createUserObj.UserIdentifier.Email, UserIdentifier = createUserObj.UserIdentifier.Phone ?? createUserObj.UserIdentifier.Username, UserId = user.UserId }), queryAdditionalIds: true) ||
                    (!createUserObj.UserIdentifier.Phone.IsNullOrEmpty() && await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserIdentifier = createUserObj.UserIdentifier.Phone }), queryAdditionalIds: true)) ||
                    (!createUserObj.UserIdentifier.Username.IsNullOrEmpty() && await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserIdentifier = createUserObj.UserIdentifier.Username }), queryAdditionalIds: true)) ||
                    (!user.UserId.IsNullOrEmpty() && await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserId = user.UserId }), queryAdditionalIds: true)))
                {
                    throw new UserExistsException($"User '{createUserObj.UserIdentifier.ToJson()}' already exists.") { UserIdentifier = createUserObj.UserIdentifier };
                }

                if (requerePassword)
                {
                    await ValidatePasswordPolicyAsync(createUserObj.UserIdentifier, createUserObj.Password);
                }
            }
            if (!(createUserObj.PasswordlessEmail || createUserObj.PasswordlessSms))
            {
                user.ChangePassword = createUserObj.ChangePassword;
            }
            await tenantDataRepository.CreateAsync(user);

            logger.ScopeTrace(() => $"User '{createUserObj.UserIdentifier.ToJson()}' created, with user id '{user.UserId}'.");

            return user;
        }

        private async Task SetIdsAsync(User user, string tenantName, string trackName)
        {
            await user.SetIdAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, Email = user.Email, UserId = user.UserId });
            if (!user.Email.IsNullOrEmpty())
            {
                await user.SetAdditionalIdAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserIdentifier = user.Email });
            }
            if (!user.Phone.IsNullOrEmpty())
            {
                await user.SetAdditionalIdAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserIdentifier = user.Phone });
            }
            if (!user.Username.IsNullOrEmpty())
            {
                await user.SetAdditionalIdAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserIdentifier = user.Username });
            }
        }

        public async Task<User> ChangePasswordUserAsync(UserIdentifier userIdentifier, string currentPassword, string newPassword)
        {
            userIdentifier.Email = userIdentifier.Email?.Trim().ToLower();
            userIdentifier.Phone = userIdentifier.Phone?.Trim();
            userIdentifier.Username = userIdentifier.Username?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Change password user '{userIdentifier.ToJson()}', Route '{RouteBinding?.Route}'.");

            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = userIdentifier.Email, UserIdentifier = userIdentifier.Phone ?? userIdentifier.Username });
            var user = await tenantDataRepository.GetAsync<User>(id, required: false);

            if (user == null || user.DisableAccount)
            {
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(currentPassword);
                throw new UserNotExistsException($"User '{userIdentifier.ToJson()}' do not exist or is disabled, trying to change password.");
            }

            if (user.PasswordlessEmail || user.PasswordlessSms)
            {
                throw new Exception($"Passwordless user with user id '{user.UserId}' can not change password.");
            }

            logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}' exists, with user id '{user.UserId}', trying to change password.");
            if (await secretHashLogic.ValidateSecretAsync(user, currentPassword))
            {
                logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}', current password valid, changing password.", triggerEvent: true);

                if (currentPassword.Equals(newPassword, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NewPasswordEqualsCurrentException($"New password equals current password, user '{userIdentifier.ToJson()}'.");
                }

                await ValidatePasswordPolicyAsync(userIdentifier, newPassword);

                await secretHashLogic.AddSecretHashAsync(user, newPassword);
                user.ChangePassword = false;
                await tenantDataRepository.SaveAsync(user);

                logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}', password changed.", triggerEvent: true);
                return user;
            }
            else
            {
                throw new InvalidPasswordException($"Current password invalid, user '{userIdentifier.ToJson()}'.");
            }
        }

        public async Task SetPasswordUserAsync(User user, string newPassword)
        {
            var userIdentifier = new UserIdentifier { Email = user.Email, Phone = user.Phone, Username = user.Username };
            logger.ScopeTrace(() => $"Set password user '{userIdentifier.ToJson()}', Route '{RouteBinding?.Route}'.");

            if (user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{userIdentifier.ToJson()}' is disabled, trying to set password.");
            }

            await ValidatePasswordPolicyAsync(userIdentifier, newPassword);

            await secretHashLogic.AddSecretHashAsync(user, newPassword);
            user.PasswordlessEmail = false;
            user.PasswordlessSms = false;
            user.ChangePassword = false;
            user.SetPasswordEmail = false;
            user.SetPasswordSms = false;
            await tenantDataRepository.SaveAsync(user);

            logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}', password set.", triggerEvent: true);
        }

        protected async Task ValidatePasswordPolicyAsync(UserIdentifier userIdentifier, string password)
        {
            CheckPasswordLength(password);

            if (RouteBinding.CheckPasswordComplexity)
            {
                CheckPasswordComplexity(userIdentifier, password);
            }

            if (RouteBinding.CheckPasswordRisk)
            {
                await CheckPasswordRiskAsync(password);
            }
        }

        protected async Task ValidatePasswordRiskAsync(string password)
        {
            if (RouteBinding.CheckPasswordRisk)
            {
                await CheckPasswordRiskAsync(password);
            }
        }

        private void CheckPasswordLength(string password)
        {
            if (password.Length < RouteBinding.PasswordLength)
            {
                throw new PasswordLengthException("Password is to short.");
            }
        }

        private void CheckPasswordComplexity(UserIdentifier userIdentifier, string password)
        {
            CheckPasswordComplexityCharRepeat(password);
            CheckPasswordComplexityCharDissimilarity(password);
            if (!userIdentifier.Email.IsNullOrEmpty())
            {
                CheckPasswordComplexityContainsEmail(userIdentifier.Email, password);
            }
            if (!userIdentifier.Phone.IsNullOrEmpty())
            {
                CheckPasswordComplexityContainsPhone(userIdentifier.Phone, password);
            }
            if (!userIdentifier.Username.IsNullOrEmpty())
            {
                CheckPasswordComplexityContainsUsername(userIdentifier.Username, password);
            }
            CheckPasswordComplexityContainsUrl(password);
        }

        private void CheckPasswordComplexityCharRepeat(string password)
        {
            var maxCharRepeate = password.Length  / 2;
            maxCharRepeate = maxCharRepeate < 3 ? 3 : maxCharRepeate;

            var charCounts = password.GroupBy(c => c).Select(g => g.Count());
            if(charCounts.Any(c => c >= maxCharRepeate))
            {
                throw new PasswordComplexityException("Password char repeat does not comply with complexity requirements.");
            }
        }

        private void CheckPasswordComplexityCharDissimilarity(string password)
        {
            var matchCount = 0;
            if (Regex.IsMatch(password, @"[a-z]")) matchCount++;
            if (Regex.IsMatch(password, @"[A-Z]")) matchCount++;
            if (Regex.IsMatch(password, @"\d")) matchCount++;
            if (Regex.IsMatch(password, @"\W")) matchCount++;
            if (matchCount < 3)
            {
                throw new PasswordComplexityException("Password char dissimilarity does not comply with complexity requirements.");
            }
        }

        private void CheckPasswordComplexityContainsEmail(string email, string password)
        {
            var emailSplit = email.Split('@', '.', '-', '_');
            foreach (var es in emailSplit)
            {
                if (es.Length > 3 && password.Contains(es, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PasswordEmailTextComplexityException($"Password contains parts of the email '{email}' which does not comply with complexity requirements.");
                }
            }
        }

        private void CheckPasswordComplexityContainsPhone(string phone, string password)
        {
            var phoneTrim = phone.TrimStart('+');
            if (phoneTrim.Length > 3 && password.Contains(phoneTrim, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new PasswordPhoneTextComplexityException($"Password contains the phone number '{phone}' which does not comply with complexity requirements.");
            }
        }

        private void CheckPasswordComplexityContainsUsername(string username, string password)
        {
            var usernameSplit = username.Split('@', '.', '-', '_', ':');
            foreach (var us in usernameSplit)
            {
                if (us.Length > 3 && password.Contains(us, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PasswordUsernameTextComplexityException($"Password contains parts of the username '{username}' which does not comply with complexity requirements.");
                }
            }
        }

        private void CheckPasswordComplexityContainsUrl(string password)
        {
            var url = $"{HttpContext.GetHost(false)}{HttpContext.Request.Path.Value}";
            var urlSplit = url.Substring(0, url.LastIndexOf('/')).Split(':', '/', '(', ')', '.', '-', '_');
            foreach (var us in urlSplit)
            {
                if (us.Length > 3 && password.Contains(us, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PasswordUrlTextComplexityException("Password contains parts of the URL which does not comply with complexity requirements.");
                }
            }
        }

        private async Task CheckPasswordRiskAsync(string password)
        {
            var passwordSha1Hash = password.Sha1Hash();
            if (await masterDataRepository.ExistsAsync<RiskPassword>(await RiskPassword.IdFormatAsync(new RiskPassword.IdKey { PasswordSha1Hash = passwordSha1Hash })))
            {
                throw new PasswordRiskException("Password has appeared in a data breach and is at risk.");
            }
        }
    }
}
