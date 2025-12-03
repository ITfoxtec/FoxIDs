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

        protected PasswordPolicyState GetPasswordPolicy(User user)
        {
            if (user != null && !user.PasswordPolicyName.IsNullOrWhiteSpace() && RouteBinding.PasswordPolicies?.Count > 0)
            {
                var group = RouteBinding.PasswordPolicies.FirstOrDefault(p => p.Name == user.PasswordPolicyName);
                if (group != null)
                {
                    return group;
                }
            }

            return new PasswordPolicyState
            {
                Length = RouteBinding.PasswordLength,
                MaxLength = RouteBinding.PasswordMaxLength,
                CheckComplexity = RouteBinding.CheckPasswordComplexity,
                CheckRisk = RouteBinding.CheckPasswordRisk,
                BannedCharacters = RouteBinding.PasswordBannedCharacters,
                History = RouteBinding.PasswordHistory,
                MaxAge = RouteBinding.PasswordMaxAge,
                SoftChange = RouteBinding.SoftPasswordChange
            };
        }

        public async Task<User> CreateUserAsync(CreateUserObj createUserObj, bool checkUserAndPasswordPolicy = true, string tenantName = null, string trackName = null, bool saveUser = true)
        {
            createUserObj.UserIdentifier.Email = createUserObj.UserIdentifier.Email?.Trim().ToLower();
            createUserObj.UserIdentifier.Email = !createUserObj.UserIdentifier.Email.IsNullOrEmpty() ? createUserObj.UserIdentifier.Email : null;
            createUserObj.UserIdentifier.Phone = createUserObj.UserIdentifier.Phone?.Trim();
            createUserObj.UserIdentifier.Phone = !createUserObj.UserIdentifier.Phone.IsNullOrEmpty() ? createUserObj.UserIdentifier.Phone : null;
            createUserObj.UserIdentifier.Username = createUserObj.UserIdentifier.Username?.Trim()?.ToLower();
            createUserObj.UserIdentifier.Username = !createUserObj.UserIdentifier.Username.IsNullOrEmpty() ? createUserObj.UserIdentifier.Username : null;
            createUserObj.PasswordPolicyName = createUserObj.PasswordPolicyName?.Trim()?.ToLower();
            logger.ScopeTrace(() => $"Creating user '{createUserObj.UserIdentifier.ToJson()}', Route '{RouteBinding?.Route}'.");

            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Email = createUserObj.UserIdentifier.Email,
                Phone = createUserObj.UserIdentifier.Phone,
                Username = createUserObj.UserIdentifier.Username,
                ConfirmAccount = createUserObj.ConfirmAccount,
                ChangePassword = createUserObj.ChangePassword,
                SetPasswordSms = createUserObj.SetPasswordSms,
                SetPasswordEmail = createUserObj.SetPasswordEmail,
                DisableSetPasswordSms = createUserObj.DisableSetPasswordSms,
                DisableSetPasswordEmail = createUserObj.DisableSetPasswordEmail,
                PhoneVerified = createUserObj.PhoneVerified,
                EmailVerified = createUserObj.EmailVerified,
                DisableAccount = createUserObj.DisableAccount,
                RequireMultiFactor = createUserObj.RequireMultiFactor,
                DisableTwoFactorApp = createUserObj.DisableTwoFactorApp,
                DisableTwoFactorSms = createUserObj.DisableTwoFactorSms,
                DisableTwoFactorEmail = createUserObj.DisableTwoFactorEmail,
                PasswordPolicyName = createUserObj.PasswordPolicyName
            };

            tenantName = tenantName ?? RouteBinding.TenantName;
            trackName = trackName ?? RouteBinding.TrackName;
            await SetIdsAsync(user, tenantName, trackName);

            if (!createUserObj.Password.IsNullOrWhiteSpace())
            {
                await secretHashLogic.AddSecretHashAsync(user, createUserObj.Password);
                user.PasswordLastChanged = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
                if (saveUser)
                {
                    if (await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, Email = createUserObj.UserIdentifier.Email, UserIdentifier = createUserObj.UserIdentifier.Phone ?? createUserObj.UserIdentifier.Username, UserId = user.UserId }), queryAdditionalIds: true) ||
                        (!createUserObj.UserIdentifier.Phone.IsNullOrEmpty() && await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserIdentifier = createUserObj.UserIdentifier.Phone }), queryAdditionalIds: true)) ||
                        (!createUserObj.UserIdentifier.Username.IsNullOrEmpty() && await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserIdentifier = createUserObj.UserIdentifier.Username }), queryAdditionalIds: true)) ||
                        (!user.UserId.IsNullOrEmpty() && await tenantDataRepository.ExistsAsync<User>(await User.IdFormatAsync(new User.IdKey { TenantName = tenantName, TrackName = trackName, UserId = user.UserId }), queryAdditionalIds: true)))
                    {
                        throw new UserExistsException($"User '{createUserObj.UserIdentifier.ToJson()}' already exists.") { UserIdentifier = createUserObj.UserIdentifier };
                    }
                }

                if (!createUserObj.Password.IsNullOrWhiteSpace())
                {
                    await ValidatePasswordPolicyAndNotifyAsync(createUserObj.UserIdentifier, createUserObj.Password, PasswordState.New, user, GetPasswordPolicy(user));
                }
            }

            if (saveUser)
            {
                await tenantDataRepository.CreateAsync(user);
                logger.ScopeTrace(() => $"User '{createUserObj.UserIdentifier.ToJson()}' created, with user id '{user.UserId}'.");
            }

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

            if (user.Hash.IsNullOrWhiteSpace())
            {
                throw new Exception($"User with user id '{user.UserId}' can not change password, because the user do not have a password.");
            }

            logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}' exists, with user id '{user.UserId}', trying to change password.");
            if (await secretHashLogic.ValidateSecretAsync(user, currentPassword))
            {
                logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}', current password valid, changing password.", triggerEvent: true);

                if (currentPassword.Equals(newPassword, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NewPasswordEqualsCurrentException($"New password equals current password, user '{userIdentifier.ToJson()}'.");
                }

                var passwordPolicy = GetPasswordPolicy(user);
                await ValidatePasswordPolicyAndNotifyAsync(userIdentifier, newPassword, PasswordState.New, user, passwordPolicy);

                await UpdatePasswordHistoryAsync(user, currentPassword, passwordPolicy);
                await secretHashLogic.AddSecretHashAsync(user, newPassword);
                user.PasswordLastChanged = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                user.SoftPasswordChangeStarted = 0;
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

            var passwordPolicy = GetPasswordPolicy(user);
            await ValidatePasswordPolicyAndNotifyAsync(userIdentifier, newPassword, PasswordState.New, user, passwordPolicy);

            if (!await secretHashLogic.ValidateSecretAsync(user, newPassword))
            {
                // Only update password history and last changed if the new password is different from the current password.
                await UpdatePasswordHistoryAsync(user, null, passwordPolicy);
                user.PasswordLastChanged = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            await secretHashLogic.AddSecretHashAsync(user, newPassword);
            user.SoftPasswordChangeStarted = 0;
            user.ChangePassword = false;
            user.SetPasswordEmail = false;
            user.SetPasswordSms = false;
            await tenantDataRepository.SaveAsync(user);

            logger.ScopeTrace(() => $"User '{userIdentifier.ToJson()}', password set.", triggerEvent: true);
        }

        protected virtual async Task ValidatePasswordPolicyAndNotifyAsync(UserIdentifier userIdentifier, string password, PasswordState state, User user, PasswordPolicyState policy)
        {
            CheckPasswordLength(password, policy);
            CheckPasswordBannedCharacters(password, policy);

            if (policy.CheckComplexity)
            {
                CheckPasswordComplexity(userIdentifier, password, policy);
            }

            if (policy.CheckRisk)
            {
                await CheckPasswordRiskAsync(password, policy);
            }

            if (state != PasswordState.Current)
            {
                await CheckPasswordHistoryAsync(user, password, policy);
            }
            else
            {
                CheckPasswordMaxAge(user, policy);
            }
        }

        private void CheckPasswordLength(string password, PasswordPolicyState policy)
        {
            if (password.Length < policy.Length)
            {
                throw new PasswordLengthException("Password is to short.") { PasswordPolicy = policy };
            }
            if (password.Length > policy.MaxLength)
            {
                throw new PasswordMaxLengthException("Password is to long.") { PasswordPolicy = policy };
            }
        }

        private void CheckPasswordBannedCharacters(string password, PasswordPolicyState policy)
        {
            if (!policy.BannedCharacters.IsNullOrWhiteSpace())
            {
                foreach (var c in policy.BannedCharacters)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        continue;
                    }
                    if (password.Contains(c, StringComparison.Ordinal))
                    {
                        throw new PasswordBannedCharactersException("Password contains banned characters.") { PasswordPolicy = policy };
                    }
                }
            } 
        }

        private void CheckPasswordComplexity(UserIdentifier userIdentifier, string password, PasswordPolicyState policy)
        {
            CheckPasswordComplexityCharRepeat(password, policy);
            CheckPasswordComplexityCharDissimilarity(password, policy);
            if (!userIdentifier.Email.IsNullOrEmpty())
            {
                CheckPasswordComplexityContainsEmail(userIdentifier.Email, password, policy);
            }
            if (!userIdentifier.Phone.IsNullOrEmpty())
            {
                CheckPasswordComplexityContainsPhone(userIdentifier.Phone, password, policy);
            }
            if (!userIdentifier.Username.IsNullOrEmpty())
            {
                CheckPasswordComplexityContainsUsername(userIdentifier.Username, password, policy);
            }
            CheckPasswordComplexityContainsUrl(password, policy);
        }

        private void CheckPasswordComplexityCharRepeat(string password, PasswordPolicyState policy)
        {
            var maxCharRepeate = password.Length  / 2;
            maxCharRepeate = maxCharRepeate < 3 ? 3 : maxCharRepeate;

            var charCounts = password.GroupBy(c => c).Select(g => g.Count());
            if(charCounts.Any(c => c >= maxCharRepeate))
            {
                throw new PasswordComplexityException("Password char repeat does not comply with complexity requirements.") { PasswordPolicy = policy };
            }
        }

        private void CheckPasswordComplexityCharDissimilarity(string password, PasswordPolicyState policy)
        {
            var matchCount = 0;
            if (Regex.IsMatch(password, @"[a-z]")) matchCount++;
            if (Regex.IsMatch(password, @"[A-Z]")) matchCount++;
            if (Regex.IsMatch(password, @"\d")) matchCount++;
            if (Regex.IsMatch(password, @"\W")) matchCount++;
            if (matchCount < 3)
            {
                throw new PasswordComplexityException("Password char dissimilarity does not comply with complexity requirements.") { PasswordPolicy = policy };
            }
        }

        private void CheckPasswordComplexityContainsEmail(string email, string password, PasswordPolicyState policy)
        {
            var emailSplit = email.Split('@', '.', '-', '_');
            foreach (var es in emailSplit)
            {
                if (es.Length > 3 && password.Contains(es, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PasswordEmailTextComplexityException($"Password contains parts of the email '{email}' which does not comply with complexity requirements.") { PasswordPolicy = policy };
                }
            }
        }

        private void CheckPasswordComplexityContainsPhone(string phone, string password, PasswordPolicyState policy)
        {
            var phoneTrim = phone.TrimStart('+');
            if (phoneTrim.Length > 3 && password.Contains(phoneTrim, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new PasswordPhoneTextComplexityException($"Password contains the phone number '{phone}' which does not comply with complexity requirements.") { PasswordPolicy = policy };
            }
        }

        private void CheckPasswordComplexityContainsUsername(string username, string password, PasswordPolicyState policy)
        {
            var usernameSplit = username.Split('@', '.', '-', '_', ':');
            foreach (var us in usernameSplit)
            {
                if (us.Length > 3 && password.Contains(us, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PasswordUsernameTextComplexityException($"Password contains parts of the username '{username}' which does not comply with complexity requirements.") { PasswordPolicy = policy };
                }
            }
        }

        private void CheckPasswordComplexityContainsUrl(string password, PasswordPolicyState policy)
        {
            var url = $"{HttpContext.GetHost(false)}{HttpContext.Request.Path.Value}";
            var urlSplit = url.Substring(0, url.LastIndexOf('/')).Split(':', '/', '(', ')', '.', '-', '_');
            foreach (var us in urlSplit)
            {
                if (us.Length > 3 && password.Contains(us, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PasswordUrlTextComplexityException("Password contains parts of the URL which does not comply with complexity requirements.") { PasswordPolicy = policy };
                }
            }
        }

        private async Task CheckPasswordHistoryAsync(User user, string password, PasswordPolicyState policy)
        {
            if (user == null || policy.History <= 0)
            {
                return;
            }

            if (user.PasswordHistory?.Count > 0)
            {
                var passwordHash = await secretHashLogic.GetPasswordHistoryHashAsync(password);
                foreach (var history in user.PasswordHistory.Take(policy.History))
                {
                    if (await secretHashLogic.ValidatePasswordHistoryHashAsync(history, password, passwordHash))
                    {
                        throw new PasswordHistoryException("Password reuse detected.") { PasswordPolicy = policy };
                    }
                }
            }
        }

        protected async Task UpdatePasswordHistoryAsync(User user, string password, PasswordPolicyState policy)
        {
            if (user == null)
            {
                return;
            }

            if (policy.History <= 0)
            {
                user.PasswordHistory = null;
                return;
            }

            user.PasswordHistory ??= new List<PasswordHistoryItem>();

            PasswordHistoryItem passwordHistoryItem = new PasswordHistoryItem
            {
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (!password.IsNullOrWhiteSpace())
            {
                var passwordHash = await secretHashLogic.GetPasswordHistoryHashAsync(password);

                var filteredHistory = new List<PasswordHistoryItem>();
                foreach (var history in user.PasswordHistory)
                {
                    if (await secretHashLogic.ValidatePasswordHistoryHashAsync(history, password, passwordHash))
                    {
                        continue;
                    }
                    filteredHistory.Add(history);
                }
                user.PasswordHistory = filteredHistory;
                
                secretHashLogic.AddPasswordHistoryHash(passwordHistoryItem, passwordHash);
            }
            else if (!user.Hash.IsNullOrWhiteSpace())
            {
                secretHashLogic.CopySecretHash(user, passwordHistoryItem);
                user.PasswordHistory = user.PasswordHistory.Where(h => !(h.HashAlgorithm == passwordHistoryItem.HashAlgorithm && h.Hash == passwordHistoryItem.Hash && h.HashSalt == passwordHistoryItem.HashSalt)).ToList();
            }

            user.PasswordHistory.Insert(0, passwordHistoryItem);

            if (user.PasswordHistory.Count > policy.History)
            {
                user.PasswordHistory = user.PasswordHistory.Take(policy.History).ToList();
            }
        }

        private void CheckPasswordMaxAge(User user, PasswordPolicyState policy)
        {
            if (user == null || policy.MaxAge <= 0)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (user.PasswordLastChanged + policy.MaxAge < now)
            {
                throw new PasswordExpiredException("Password is too old.") { PasswordPolicy = policy };
            }
        }

        private async Task CheckPasswordRiskAsync(string password, PasswordPolicyState policy)
        {
            var passwordSha1Hash = password.Sha1Hash();
            if (await masterDataRepository.ExistsAsync<RiskPassword>(await RiskPassword.IdFormatAsync(new RiskPassword.IdKey { PasswordSha1Hash = passwordSha1Hash })))
            {
                throw new PasswordRiskException("Password has appeared in a data breach and is at risk.") { PasswordPolicy = policy };
            }
        }
    }
}