using ITfoxtec.Identity.Util;
using FoxIDs.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using static FoxIDs.Constants.Models;

namespace FoxIDs.Logic
{
    public class SecretHashLogic 
    {
        private static KeyDerivationPrf defaultPrf = KeyDerivationPrf.HMACSHA512;

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

            item.HashAlgorithm = SecretHash.DefaultHashAlgorithm;

            var salt = RandomGenerator.GenerateBytes(SecretHash.DefaultSaltBytes);
            var hash = KeyDerivation.Pbkdf2(secret, salt, defaultPrf, SecretHash.DefaultIterations * 10000, SecretHash.DefaultDerivedKeyBytes);

            item.HashSalt = WebEncoders.Base64UrlEncode(salt);
            item.Hash = WebEncoders.Base64UrlEncode(hash);

            return Task.FromResult(string.Empty);
        }

        public Task<bool> ValidateSecretAsync(ISecretHash item, string secret)
        {
            var iterations = VerifyAlgorithmAndGetIterations(item);

            var salt = WebEncoders.Base64UrlDecode(item.HashSalt);
            var hash = KeyDerivation.Pbkdf2(secret, salt, defaultPrf, iterations * 10000, SecretHash.DefaultDerivedKeyBytes);

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
            var salt = RandomGenerator.GenerateBytes(SecretHash.DefaultSaltBytes);
            KeyDerivation.Pbkdf2(secret, salt, defaultPrf, SecretHash.DefaultIterations * 10000, SecretHash.DefaultDerivedKeyBytes);

            return Task.FromResult(string.Empty);
        }

        private int VerifyAlgorithmAndGetIterations(ISecretHash item)
        {
            var algoSplit = item.HashAlgorithm?.Split(':');
            if (algoSplit?.Count() != 2 || algoSplit[0] != SecretHash.DefaultPostHashAlgorithm)
            {
                throw new NotSupportedException($"Password hash algorithm '{item.HashAlgorithm}' not supported. Item '{item.ToJson()}'.");
            }

            return Convert.ToInt32(algoSplit[1]);
        }
    }
}
