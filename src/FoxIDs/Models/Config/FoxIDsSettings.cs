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
        /// Persistent session max unlimited lifetime in years.
        /// </summary>
        [Required]
        public int PersistentSessionMaxUnlimitedLifetimeYears { get; set; } = 30; // years

        /// <summary>
        /// CORS preflight max age in seconds.
        /// </summary>
        [Required]
        public int CorsPreflightMaxAge { get; set; } = 300; // 5 minutes

        /// <summary>
        /// Add time before where the SAML 2.0 token is valid in seconds.
        /// </summary>
        [Required]
        public int SamlTokenAddNotBeforeTime { get; set; } = 30; // 30 seconds

        /// <summary>
        /// Account action sequence lifetime in seconds.
        /// </summary>
        [Required]
        public int AccountActionSequenceLifetime { get; set; } = 7776000; // 90 days,

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
        /// Should the sequences be deleted after they have been used, default not deleted.
        /// </summary>
        public bool DeleteUsedSequences { get; set; }

        /// <summary>
        /// Max number of times to send a code with SMS or email. The number is added to RouteBinding.MaxFailingLogins.
        /// </summary>
        public int MaxSendingCodes { get; set; } = 2;

        /// <summary>
        /// Confirmation code lifetime in seconds, send in email.
        /// </summary>
        [Required]
        public int ConfirmationCodeEmailLifetime { get; set; } = 7200; // 2 hours

        /// <summary>
        /// Confirmation code lifetime in seconds, send in SMS.
        /// </summary>
        [Required]
        public int ConfirmationCodeSmsLifetime { get; set; } = 3600; // 1 hours

        /// <summary>
        /// Two-factor code lifetime in seconds, send in email.
        /// </summary>
        [Required]
        public int TwoFactorCodeEmailLifetime { get; set; } = 1200; // 20 minutes

        /// <summary>
        /// Two-factor code lifetime in seconds, send in SMS.
        /// </summary>
        [Required]
        public int TwoFactorCodeSmsLifetime { get; set; } = 900; // 15 minutes

        /// <summary>
        /// Authentication method update with OIDC Discovery or SAML 2.0 Metadata wait period in seconds.
        /// </summary>
        [Required]
        public int UpPartyUpdateWaitPeriod { get; set; } = 900; // 15 minutes

        /// <summary>
        /// Authentication method max failing update with OIDC Discovery or SAML 2.0 Metadata before automatic update is stopped.
        /// </summary>
        [Required]
        public int UpPartyMaxFailingUpdate { get; set; } = 8;

        /// <summary>
        /// The max number of authentication methods in the HRD cookie.
        /// </summary>
        [Required]
        public int HrdUpPartiesMaxCount { get; set; } = 5;

        /// <summary>
        /// Optional trust proxy headers. Default false. Trust and accept headers received without requiring a proxy secret.
        /// </summary>
        public bool TrustProxyHeaders { get; set; }

        /// <summary>
        /// Read the HTTP request domain and use it as custom domain if configured on a tenant.
        /// </summary>
        public bool RequestDomainAsCustomDomain { get; set; }

        /// <summary>
        /// Also read the HTTP request domain if it is called locally (localhost - 127.0.0.1).
        /// </summary>
        public bool ReadLoopbackRequestDomain { get; set; }

        /// <summary>
        /// Add domain to ignore if received in the proxy header. Used for the default domain if the domain is not attaches to a tenant but is used as the generic domain.
        /// </summary>
        public string IgnoreProxyHeaderDomain { get; set; }
    }
}
