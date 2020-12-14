using ITfoxtec.Identity.Util;
using FoxIDs.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class SecretHashLogic : LogicBase
    {
        private static string defaultHashAlgorithm = "P2HS512";
        private static int defaultSaltBytes = 64;
        private static KeyDerivationPrf defaultPrf = KeyDerivationPrf.HMACSHA512;
        private static int defaultIterations = 10;
        private static int defaultDerivedKeyBytes = 80;

        public SecretHashLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public Task AddSecretHashAsync(ISecretHash item, string secret)
        {
            if (item is OAuthClientSecret)
            {
                (item as OAuthClientSecret).Id = Guid.NewGuid().ToString();
                if (secret.Length > 20)
                {
                    (item as OAuthClientSecret).Info = secret.Substring(0, 3);
                }
            }

            item.HashAlgorithm = $"{defaultHashAlgorithm}:{defaultIterations}";

            var salt = RandomGenerator.GenerateBytes(defaultSaltBytes);
            var hash = KeyDerivation.Pbkdf2(secret, salt, defaultPrf, defaultIterations * 10000, defaultDerivedKeyBytes);

            item.HashSalt = WebEncoders.Base64UrlEncode(salt);
            item.Hash = WebEncoders.Base64UrlEncode(hash);

            return Task.FromResult(string.Empty);
        }

        public Task<bool> ValidateSecretAsync(ISecretHash item, string secret)
        {
            var iterations = VerifyAlgorithmAndGetIterations(item);

            var salt = WebEncoders.Base64UrlDecode(item.HashSalt);
            var hash = KeyDerivation.Pbkdf2(secret, salt, defaultPrf, iterations * 10000, defaultDerivedKeyBytes);

            if (WebEncoders.Base64UrlEncode(hash) == item.Hash)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task ValidateSecretDefaultTimeUsageAsync(string secret)
        {
            var salt = RandomGenerator.GenerateBytes(defaultSaltBytes);
            KeyDerivation.Pbkdf2(secret, salt, defaultPrf, defaultIterations * 10000, defaultDerivedKeyBytes);

            return Task.FromResult(string.Empty);
        }

        private int VerifyAlgorithmAndGetIterations(ISecretHash item)
        {
            var algoSplit = item.HashAlgorithm?.Split(':');
            if (algoSplit?.Count() != 2 || algoSplit[0] != defaultHashAlgorithm)
            {
                throw new NotSupportedException($"Password hash algorithm '{item.HashAlgorithm}' not supported. Item '{item.ToJsonIndented()}'.");
            }

            return Convert.ToInt32(algoSplit[1]);
        }
    }
}
