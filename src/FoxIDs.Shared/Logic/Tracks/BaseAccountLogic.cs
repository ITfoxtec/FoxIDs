using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        public async Task<User> CreateUser(UserIdentifier userIdentifier, string password, bool changePassword = false, List<Claim> claims = null, string tenantName = null, string trackName = null, bool checkUserAndPasswordPolicy = true, bool confirmAccount = true, bool emailVerified = false, bool phoneVerified = false, bool disableAccount = false, bool requireMultiFactor = false)
        {
            userIdentifier.Email = userIdentifier.Email?.Trim().ToLower();
            userIdentifier.Phone = userIdentifier.Phone?.Trim();
            userIdentifier.Username = userIdentifier.Username?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Creating user '{userIdentifier.ToJson()}', Route '{RouteBinding?.Route}'.");

            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Email = userIdentifier.Email,
                Phone = userIdentifier.Phone,
                Username = userIdentifier.Username,
                ConfirmAccount = confirmAccount,
                EmailVerified = emailVerified,
                PhoneVerified = phoneVerified,
                DisableAccount = disableAccount,
                RequireMultiFactor = requireMultiFactor
            };

            tenantName = tenantName ?? RouteBinding.TenantName;
            trackName = trackName ?? RouteBinding.TrackName;
            await SetIdsAsync(user, tenantName, trackName);

            await secretHashLogic.AddSecretHashAsync(user, password);
            if (claims?.Count() > 0)
            {
                var userIdentifierClaimTypes = new List<string>();
                if (!userIdentifier.Email.IsNullOrEmpty())
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.Email);
                }
                if (!userIdentifier.Phone.IsNullOrEmpty())
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.PhoneNumber);
                }
                if (!userIdentifier.Username.IsNullOrEmpty())
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.PreferredUsername);
                }
                claims = claims.Where(c => !userIdentifierClaimTypes.Where(t => t == c.Type).Any()).ToList();
                user.Claims = claims.ToClaimAndValues();
            }

            if (checkUserAndPasswordPolicy)
            {
                if (await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, Email = userIdentifier.Email, UserIdentifier = userIdentifier.Phone ?? userIdentifier.Username, UserId = user.UserId })))
                {
                    throw new UserExistsException($"User '{userIdentifier.ToJson()}' already exists.") { UserIdentifier = userIdentifier };
                }
                await ValidatePasswordPolicy(userIdentifier, password);
            }
            user.ChangePassword = changePassword;
            await tenantDataRepository.CreateAsync(user);

            logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}' created, with user id '{user.UserId}'.");

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

        public async Task<User> ChangePasswordUser(UserIdentifier userIdentifier, string currentPassword, string newPassword)
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

            logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}' exists, with user id '{user.UserId}', trying to change password.");
            if (await secretHashLogic.ValidateSecretAsync(user, currentPassword))
            {
                logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}', current password valid, changing password.", triggerEvent: true);

                if (currentPassword.Equals(newPassword, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NewPasswordEqualsCurrentException($"New password equals current password, user '{userIdentifier.ToJson()}'.");
                }

                await ValidatePasswordPolicy(userIdentifier, newPassword);

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

        public async Task SetPasswordUser(User user, string newPassword)
        {
            var userIdentifier = new UserIdentifier { Email = user.Email, Phone = user.Phone, Username = user.Username };
            logger.ScopeTrace(() => $"Set password user '{userIdentifier.ToJson()}', Route '{RouteBinding?.Route}'.");

            if (user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{userIdentifier.ToJson()}' is disabled, trying to set password.");
            }

            await ValidatePasswordPolicy(userIdentifier, newPassword);

            await secretHashLogic.AddSecretHashAsync(user, newPassword);
            user.ChangePassword = false;
            await tenantDataRepository.SaveAsync(user);

            logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}', password set.", triggerEvent: true);
        }

        protected async Task ValidatePasswordPolicy(UserIdentifier userIdentifier, string password)
        {
            CheckPasswordLength(password);

            if (RouteBinding.CheckPasswordComplexity)
            {
                CheckPasswordComplexity(userIdentifier, password);
            }

            if (RouteBinding.CheckPasswordRisk)
            {
                await CheckPasswordRisk(password);
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

        private async Task CheckPasswordRisk(string password)
        {
            var passwordSha1Hash = password.Sha1Hash();
            if (await masterDataRepository.ExistsAsync<RiskPassword>(await RiskPassword.IdFormatAsync(new RiskPassword.IdKey { PasswordSha1Hash = passwordSha1Hash })))
            {
                throw new PasswordRiskException("Password has appeared in a data breach and is at risk.");
            }
        }
    }
}
