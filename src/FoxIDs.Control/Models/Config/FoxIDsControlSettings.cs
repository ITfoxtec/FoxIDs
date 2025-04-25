using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Config
{
    public class FoxIDsControlSettings : Settings, IValidatableObject
    {
        /// <summary>
        /// FoxIDs backend endpoint, optionally used in FoxIDs Control to keep the communication from FoxIDs Control to FoxIDs in the backend network.
        /// </summary>
        public string FoxIDsBackendEndpoint { get; set; }

        [Required]
        public string DownParty { get; set; } = Constants.ControlApi.ResourceName;

        /// <summary>
        /// Optional, OpenSearch Query configuration.
        /// </summary>
        [ValidateComplexType]
        public OpenSearchQuerySettings OpenSearchQuery { get; set; }

        [ValidateComplexType]
        public ApplicationInsightsSettings ApplicationInsights { get; set; }

        /// <summary>
        /// Enable master seed if true.
        /// </summary>
        public bool MasterSeedEnabled { get; set; }

        /// <summary>
        /// Seed main tenant if true. At the same time as master seed is carried out.
        /// </summary>
        public bool MainTenantSeedEnabled { get; set; }

        /// <summary>
        /// Down-party test lifetime in seconds. Default 15 minutes.
        /// </summary>
        [Required]
        public int DownPartyTestLifetime { get; set; } = 900; // 15 minutes

        [ValidateComplexType]
        public PaymentSettings Payment { get; set; } 

        [ValidateComplexType]
        public UsageBaseSettings Usage { get; set; }

        public string SupportEmail { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = base.Validate(validationContext).ToList();

            if (FoxIDsEndpoint.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"The field {nameof(FoxIDsEndpoint)} is required.", new[] { nameof(FoxIDsEndpoint) }));
            }
            if (FoxIDsControlEndpoint.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"The field {nameof(FoxIDsControlEndpoint)} is required.", new[] { nameof(FoxIDsControlEndpoint) }));
            }

            if (Options.Log == LogOptions.ApplicationInsights)
            {
                if (ApplicationInsights == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(ApplicationInsights)} is required if {nameof(Options.Log)} is {Options.Log}.", new[] { nameof(ApplicationInsights) }));
                }
            }

            return results;
        }
    }
}
