using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Track : INameValue, IValidatableObject
    {
        /// <summary>
        /// Name of the track. If empty the name is auto generated.
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// Display name.
        /// </summary>
        [MaxLength(Constants.Models.Track.DisplayNameLength)]
        [RegularExpression(Constants.Models.Track.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [Range(Constants.Models.Track.SequenceLifetimeMin, Constants.Models.Track.SequenceLifetimeMax)] // 30 seconds to 5 hours. Default 2 hours.
        public int SequenceLifetime { get; set; } = 7200;

        [Range(Constants.Models.Track.MaxFailingLoginsMin, Constants.Models.Track.MaxFailingLoginsMax)]
        public int MaxFailingLogins { get; set; } = 5;

        [Range(Constants.Models.Track.FailingLoginCountLifetimeMin, Constants.Models.Track.FailingLoginCountLifetimeMax)]
        public int FailingLoginCountLifetime { get; set; } = 36000;

        [Range(Constants.Models.Track.FailingLoginObservationPeriodMin, Constants.Models.Track.FailingLoginObservationPeriodMax)]
        public int FailingLoginObservationPeriod { get; set; } = 3600;

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        public int PasswordLength { get; set; } = 6;

        [Required]
        public bool? CheckPasswordComplexity { get; set; } = true;

        [Required]
        public bool? CheckPasswordRisk { get; set; } = true;

        [ListLength(Constants.Models.Track.AllowIframeOnDomainsMin, Constants.Models.Track.AllowIframeOnDomainsMax, Constants.Models.Track.AllowIframeOnDomainsLength)]
        public List<string> AllowIframeOnDomains { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", new[] { nameof(Name), nameof(DisplayName) }));
            }
            return results;
        }
    }
}
