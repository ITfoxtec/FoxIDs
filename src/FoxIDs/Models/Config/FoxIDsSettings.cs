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
        /// Sendgrid configuration.
        /// </summary>
        public SendgridSettings Sendgrid { get; set; }

        /// <summary>
        /// SMTP configuration.
        /// </summary>
        public SmtpSettings Smtp { get; set; }

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
        /// Add time before where the SAML 2.0 token is valid in seconds.
        /// </summary>
        [Required]
        public int SamlTokenAddNotBeforeTime { get; set; }

        /// <summary>
        /// Account action sequence lifetime in seconds.
        /// </summary>
        [Required]
        public int AccountActionSequenceLifetime { get; set; }

        /// <summary>
        /// Key sequence lifetime in seconds. Default 30 seconds.
        /// </summary>
        [Required]
        public int KeySequenceLifetime { get; set; } = 30;

        /// <summary>
        /// Add a grace period to the sequence lifetime. Used in down and down link sequence data and external sequence ID to achieve graceful error handling. Default 2 hours.
        /// </summary>
        [Required]
        public int SequenceGracePeriod { get; set; } = 7200; 

        /// <summary>
        /// Confirmation code lifetime in seconds, send in email.
        /// </summary>
        [Required]
        public int ConfirmationCodeLifetime { get; set; }

        /// <summary>
        /// Authentication method update with OIDC Discovery or SAML 2.0 Metadata wait period in seconds.
        /// </summary>
        [Required]
        public int UpPartyUpdateWaitPeriod { get; set; }

        /// <summary>
        /// Authentication method max failing update with OIDC Discovery or SAML 2.0 Metadata before automatic update is stopped.
        /// </summary>
        [Required]
        public int UpPartyMaxFailingUpdate { get; set; }

        /// <summary>
        /// The max number of authentication methods in the HRD cookie.
        /// </summary>
        [Required]
        public int HrdUpPartiesMaxCount { get; set; } = 5;

        /// <summary>
        /// Optional proxy secret. Validating the HTTP header "X-FoxIDs-Secret" if not empty.
        /// </summary>
        public string ProxySecret { get; set; }

        /// <summary>
        /// Optional trust proxy headers. Default false. Trust and accept headers received without requiring a proxy secret.
        /// </summary>
        public bool TrustProxyHeaders { get; set; }

        /// <summary>
        /// Read the HTTP request domain and use it as custom domain if configured on a tenant.
        /// </summary>
        public bool RequestDomainAsCustomDomain { get; set; }
    }
}
