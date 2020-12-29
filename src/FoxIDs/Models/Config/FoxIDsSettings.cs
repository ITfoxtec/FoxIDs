using System.ComponentModel.DataAnnotations;

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
        [Required]
        public RedisCacheSettings RedisCache { get; set; }

        /// <summary>
        /// Sendgrid configuration.
        /// </summary>
        public SendgridSettings Sendgrid { get; set; }

        /// <summary>
        /// Persistent session max unlimited lifetime in years.
        /// </summary>
        [Required]
        public int PersistentSessionMaxUnlimitedLifetimeYears { get; set; }

        /// <summary>
        /// CORS preflight max age in seconds.
        /// </summary>
        [Required]
        public int CorsPreflightMaxAge { get; set; }

        /// <summary>
        /// Add time before where the token is valid in seconds.
        /// </summary>
        [Required]
        public double SamlTokenAddNotBeforeTime { get; set; }

        /// <summary>
        /// Account action sequence lifetime in seconds.
        /// </summary>
        [Required]
        public int AccountActionSequenceLifetime { get; set; }

        /// <summary>
        /// Confirmation email wait period in seconds.
        /// </summary>
        [Required]
        public int ConfirmationEmailWaitPeriod { get; set; }        
    }
}
