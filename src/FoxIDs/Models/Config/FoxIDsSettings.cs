namespace FoxIDs.Models.Config
{
    public class FoxIDsSettings : Settings
    {
        /// <summary>
        /// FoxIDs redirect to website.
        /// </summary>
        public string WebsiteUrl { get; set; }

        /// <summary>
        /// Redis Cache configuration.
        /// </summary>
        public RedisCacheSettings RedisCache { get; set; }

        /// <summary>
        /// Cookie repository, cookie threshold.
        /// </summary>
        public int CookieThreshold { get; set; }

        /// <summary>
        /// Persistent session max unlimited lifetime in years.
        /// </summary>
        public int PersistentSessionMaxUnlimitedLifetimeYears { get; set; }

        /// <summary>
        /// Cors preflight max age in secunds.
        /// </summary>
        public int CorsPreflightMaxAge { get; set; }

        /// <summary>
        /// Add extra lifetime to the sequence lifetime where sequence data is valid in secunds.
        /// </summary>
        public int SequenceDataAddLifetime { get; set; }

        /// <summary>
        /// Add time before where the token is valid in secunds.
        /// </summary>
        public double SamlTokenAddNotBeforeTime { get; set; }
    }
}
