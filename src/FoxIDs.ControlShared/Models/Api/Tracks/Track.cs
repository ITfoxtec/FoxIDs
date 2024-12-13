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
        /// Name of the environment. If empty the name is auto generated.
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

        [MaxLength(Constants.Models.Address.NameLength)]
        [Display(Name = "Company name / System name")]
        public string CompanyName { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine1Length)]
        [Display(Name = "Address line 1")]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine2Length)]
        [Display(Name = "Address line 2")]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Address.PostalCodeLength)]
        [Display(Name = "Postal code")]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Address.CityLength)]
        [Display(Name = "City")]
        public string City { get; set; }

        [MaxLength(Constants.Models.Address.StateRegionLength)]
        [Display(Name = "State / Region")]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Address.CountryLength)]
        [Display(Name = "Country")]
        public string Country { get; set; }

        [Range(Constants.Models.Track.SequenceLifetimeMin, Constants.Models.Track.SequenceLifetimeMax)] // 30 seconds to 5 hours. Default 2 hours.
        public int SequenceLifetime { get; set; } = Constants.TrackDefaults.DefaultSequenceLifetime;

        [Display(Name = "Automatically create mappings between JWT and SAML claim types")]
        public bool AutoMapSamlClaims { get; set; }

        [Range(Constants.Models.Track.MaxFailingLoginsMin, Constants.Models.Track.MaxFailingLoginsMax)]
        public int MaxFailingLogins { get; set; } = Constants.TrackDefaults.DefaultMaxFailingLogins;

        [Range(Constants.Models.Track.FailingLoginCountLifetimeMin, Constants.Models.Track.FailingLoginCountLifetimeMax)]
        public int FailingLoginCountLifetime { get; set; } = Constants.TrackDefaults.DefaultFailingLoginCountLifetime;

        [Range(Constants.Models.Track.FailingLoginObservationPeriodMin, Constants.Models.Track.FailingLoginObservationPeriodMax)]
        public int FailingLoginObservationPeriod { get; set; } = Constants.TrackDefaults.DefaultFailingLoginObservationPeriod;

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        public int PasswordLength { get; set; } = Constants.TrackDefaults.DefaultPasswordLength;

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
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
            }
            return results;
        }
    }
}
