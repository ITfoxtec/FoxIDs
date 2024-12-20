using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public async Task<User> CreateUser(string email, string password, bool changePassword = false, List<Claim> claims = null, string tenantName = null, string trackName = null, bool checkUserAndPasswordPolicy = true, bool confirmAccount = true, bool emailVerified = false, bool disableAccount = false, bool requireMultiFactor = false)
        {
            logger.ScopeTrace(() => $"Creating user '{email}', Route '{RouteBinding?.Route}'.");

            email = email?.ToLower();
            ValidateEmail(email);

            var user = new User { UserId = Guid.NewGuid().ToString(), ConfirmAccount = confirmAccount, EmailVerified = emailVerified, DisableAccount = disableAccount, RequireMultiFactor = requireMultiFactor };
            var userIdKey = new User.IdKey { TenantName = tenantName ?? RouteBinding.TenantName, TrackName = trackName ?? RouteBinding.TrackName, UserIdentifier = email?.ToLower() };
            await user.SetIdAsync(userIdKey);

            await secretHashLogic.AddSecretHashAsync(user, password);
            if (claims?.Count() > 0)
            {
                user.Claims = claims.ToClaimAndValues();
            }

            if(checkUserAndPasswordPolicy)
            {
                if (await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(userIdKey)))
                {
                    throw new UserExistsException($"User '{email}' already exists.") { Email = email };
                }
                await ValidatePasswordPolicy(email, password);
            }
            user.ChangePassword = changePassword;
            await tenantDataRepository.CreateAsync(user);

            logger.ScopeTrace(() => $"User '{email}' created, with user id '{user.UserId}'.");

            return user;
        }

        public virtual async Task<User> ChangePasswordUser(string email, string currentPassword, string newPassword)
        {
            email = email?.ToLowerInvariant();
            logger.ScopeTrace(() => $"Change password user '{email}', Route '{RouteBinding?.Route}'.");

            ValidateEmail(email);

            var id = await User.IdFormatAsync(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, UserIdentifier = email });
            var user = await tenantDataRepository.GetAsync<User>(id, required: false);

            if (user == null || user.DisableAccount)
            {
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(currentPassword);
                throw new UserNotExistsException($"User '{email}' do not exist or is disabled, trying to change password.");
            }

            logger.ScopeTrace(() => $"User '{email}' exists, with user id '{user.UserId}', trying to change password.");
            if (await secretHashLogic.ValidateSecretAsync(user, currentPassword))
            {
                logger.ScopeTrace(() => $"User '{email}', current password valid, changing password.", triggerEvent: true);

                if (currentPassword.Equals(newPassword, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NewPasswordEqualsCurrentException($"New password equals current password, user '{email}'.");
                }

                await ValidatePasswordPolicy(email, newPassword);

                await secretHashLogic.AddSecretHashAsync(user, newPassword);
                user.ChangePassword = false;
                await tenantDataRepository.SaveAsync(user);

                logger.ScopeTrace(() => $"User '{email}', password changed.", triggerEvent: true);
                return user;
            }
            else
            {
                throw new InvalidPasswordException($"Current password invalid, user '{email}'.");
            }
        }

        public async Task SetPasswordUser(User user, string newPassword)
        {
            logger.ScopeTrace(() => $"Set password user '{user.Email}', Route '{RouteBinding?.Route}'.");

            if (user.DisableAccount)
            {
                throw new UserNotExistsException($"User '{user.Email}' is disabled, trying to set password.");
            }

            await ValidatePasswordPolicy(user.Email, newPassword);

            await secretHashLogic.AddSecretHashAsync(user, newPassword);
            user.ChangePassword = false;
            await tenantDataRepository.SaveAsync(user);

            logger.ScopeTrace(() => $"User '{user.Email}', password set.", triggerEvent: true);
        }


        protected void ValidateEmail(string email)
        {
            if (!new EmailAddressAttribute().IsValid(email))
            {
                throw new InvalidEmailException($"Email '{email}' is invalid.");
            }
        }

        protected async Task ValidatePasswordPolicy(string email, string password)
        {
            CheckPasswordLength(password);

            if (RouteBinding.CheckPasswordComplexity)
            {
                CheckPasswordComplexity(email, password);
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

        private void CheckPasswordComplexity(string email, string password)
        {
            CheckPasswordComplexityCharRepeat(password);
            CheckPasswordComplexityCharDissimilarity(password);
            CheckPasswordComplexityContainsEmail(email, password);
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
                    throw new PasswordEmailTextComplexityException($"Password contains parts of the e-mail '{email}' which does not comply with complexity requirements.");
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
