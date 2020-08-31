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
    public class AccountLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly IMasterRepository masterRepository;
        private readonly SecretHashLogic secretHashLogic;

        public AccountLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, IMasterRepository masterRepository, SecretHashLogic secretHashLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.masterRepository = masterRepository;
            this.secretHashLogic = secretHashLogic;
        }

        public async Task ThrowIfUserExists(string email)
        {
            logger.ScopeTrace($"Check if user exists '{email}', Route '{RouteBinding.Route}'.");

            ValidateEmail(email);

            if (await tenantRepository.ExistsAsync<User>(await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email })))
            {
                throw new UserExistsException($"User '{email}' already exists.");
            }
        }

        public async Task<User> CreateUser(string email, string password, List<Claim> claims = null, string tenantName = null, string trackName = null)
        {
            logger.ScopeTrace($"Creating user '{email}', Route '{RouteBinding.Route}'.");

            ValidateEmail(email);

            var user = new User { UserId = Guid.NewGuid().ToString() };
            await user.SetIdAsync(new User.IdKey { TenantName = tenantName ?? RouteBinding.TenantName, TrackName = trackName ?? RouteBinding.TrackName, Email = email?.ToLower() });

            await secretHashLogic.AddSecretHashAsync(user, password);
            if (claims?.Count() > 0)
            {
                user.Claims = claims.ToClaimAndValues();
            }

            await ThrowIfUserExists(email);
            await ValidatePasswordPolicy(email, password);
            await tenantRepository.CreateAsync(user);

            logger.ScopeTrace($"User '{email}' created, with user id '{user.UserId}'.");

            return user;
        }

        public async Task<User> ValidateUser(string email, string password)
        {
            logger.ScopeTrace($"Validating user '{email}', Route '{RouteBinding.Route}'.");

            ValidateEmail(email);

            var id = await User.IdFormat(new User.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, Email = email });
            var user = await tenantRepository.GetAsync<User>(id, false);

            if (user == null)
            {
                await secretHashLogic.ValidateSecretDefaultTimeUsageAsync(password);
                throw new UserNotExistsException($"User '{email}' do not exist."); // UI message: Wrong email or password / Your email was not found
            }

            logger.ScopeTrace($"User '{email}' exists, with user id '{user.UserId}'.");
            if (await secretHashLogic.ValidateSecretAsync(user, password))
            {
                logger.ScopeTrace($"User '{email}', password valid.", triggerEvent: true);
                return user;
            }
            else
            {
                throw new InvalidPasswordException($"Password invalid, user '{email}'."); // UI message: Wrong email or password / Wrong password
            }
        }

        private void ValidateEmail(string email)
        {
            if (!new EmailAddressAttribute().IsValid(email))
            {
                throw new InvalidEmailException($"Email '{email}' is invalid.");
            }
        }

        private async Task ValidatePasswordPolicy(string email, string password)
        {
            CheckPasswordLength(email, password);

            if (RouteBinding.CheckPasswordComplexity)
            {
                CheckPasswordComplexity(email, password);
            }

            if (RouteBinding.CheckPasswordRisk)
            {
                await CheckPasswordRisk(email, password);
            }
        }

        private void CheckPasswordLength(string email, string password)
        {
            if (password.Length < RouteBinding.PasswordLength)
            {
                throw new PasswordLengthException($"Password is to short, user '{email}'.");
            }
        }

        private void CheckPasswordComplexity(string email, string password)
        {
            CheckPasswordComplexityCharRepeate(email, password);
            CheckPasswordComplexityCharDissimilarity(email, password);
            CheckPasswordComplexityContainsEmail(email, password);
            CheckPasswordComplexityContainsUrl(email, password);
        }

        private void CheckPasswordComplexityCharRepeate(string email, string password)
        {
            var maxCharRepeate = RouteBinding.PasswordLength / 2;

            var charCounts = password.GroupBy(c => c).Select(g => g.Count());
            if(charCounts.Any(c => c >= maxCharRepeate))
            {
                throw new PasswordComplexityException($"Password char repeate does not comply with complexity, user '{email}'.");
            }
        }

        private void CheckPasswordComplexityCharDissimilarity(string email, string password)
        {
            var matchCount = 0;
            if (Regex.IsMatch(password, @"[a-z]")) matchCount++;
            if (Regex.IsMatch(password, @"[A-Z]")) matchCount++;
            if (Regex.IsMatch(password, @"\d")) matchCount++;
            if (Regex.IsMatch(password, @"\W")) matchCount++;
            if (matchCount < 3)
            {
                throw new PasswordComplexityException($"Password char dissimilarity does not comply with complexity, user '{email}'.");
            }
        }

        private void CheckPasswordComplexityContainsEmail(string email, string password)
        {
            var emailSplit = email.Split('@', '.', '-', '_');
            foreach (var es in emailSplit)
            {
                if (es.Length > 3 && password.Contains(es, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PasswordEmailTextComplexityException($"Password contains e-mail text that does not comply with complexity, user '{email}'.");
                }
            }
        }

        private void CheckPasswordComplexityContainsUrl(string email, string password)
        {
            var url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToUriComponent()}{HttpContext.Request.Path.Value}";
            var urlSplit = url.Substring(0, url.LastIndexOf('/')).Split(':', '/', '(', ')', '.', '-', '_');
            foreach (var us in urlSplit)
            {
                if (us.Length > 3 && password.Contains(us, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PasswordUrlTextComplexityException($"Password contains url text that does not comply with complexity, user '{email}'.");
                }
            }
        }

        private async Task CheckPasswordRisk(string email, string password)
        {
            var passwordSha1Hash = password.Sha1Hash();
            if (await masterRepository.ExistsAsync<RiskPassword>(await RiskPassword.IdFormat(new RiskPassword.IdKey { PasswordSha1Hash = passwordSha1Hash })))
            {
                throw new PasswordRiskException($"Password has appeared in a data breach and is at risk, user '{email}'.");
            }
        }
    }
}
