namespace FoxIDs
{
    public static class SecretExtensions
    {
        /// <summary>
        /// Short secret returned in the API
        /// </summary>
        public static string GetShortSecret(this string secret, bool withDots)
        {
            if (secret != null && secret.Length > 20)
            {
                var shortSecret = secret.Substring(0, 3);
                return withDots && shortSecret.Length == 3 ? $"{shortSecret}..." : shortSecret;
            }

            return secret;
        }
    }
}
