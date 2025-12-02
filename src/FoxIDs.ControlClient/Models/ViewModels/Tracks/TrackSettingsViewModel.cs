using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TrackSettingsViewModel
    {
        public TrackSettingsViewModel()
        {
            AllowIframeOnDomains = new List<string>();
            PasswordPolicies = new List<PasswordPolicyViewModel>();
        }

        [MaxLength(Constants.Models.Track.DisplayNameLength)]
        [RegularExpression(Constants.Models.Track.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Address.NameLength)]
        [Display(Name = "Company name / Name")]
        public string CompanyName { get; set; }

        [MaxLength(Constants.Models.Address.VatNumberLength)]
        [Display(Name = "VAT number")]
        public string VatNumber { get; set; }

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


        [Range(Constants.Models.Track.SequenceLifetimeMin, Constants.Models.Track.SequenceLifetimeMax)] 
        [Display(Name = "Sequence lifetime")]
        public int SequenceLifetime { get; set; }

        [Display(Name = "Automatically create mappings between JWT and SAML claim types")]
        public bool AutoMapSamlClaims { get; set; }

        [Range(Constants.Models.Track.MaxFailingLoginsMin, Constants.Models.Track.MaxFailingLoginsMax)]
        [Display(Name = "Lockout count")]
        public int MaxFailingLogins { get; set; } = 5;

        [Range(Constants.Models.Track.FailingLoginCountLifetimeMin, Constants.Models.Track.FailingLoginCountLifetimeMax)]
        [Display(Name = "Lockout count timeframe")]
        public int FailingLoginCountLifetime { get; set; } = 36000;

        [Range(Constants.Models.Track.FailingLoginObservationPeriodMin, Constants.Models.Track.FailingLoginObservationPeriodMax)]
        [Display(Name = "Lockout period")]
        public int FailingLoginObservationPeriod { get; set; } = 3600;

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password min length")]
        public int PasswordLength { get; set; } = Constants.TrackDefaults.DefaultPasswordLength;

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password max length")]
        public int PasswordMaxLength { get; set; } = Constants.Models.Track.PasswordLengthMax;

        [Required]
        [Display(Name = "Check password complexity")]
        public bool? CheckPasswordComplexity { get; set; }

        [Required]
        [Display(Name = "Check password risk based on global password breaches")]
        public bool? CheckPasswordRisk { get; set; }

        [MaxLength(Constants.Models.Track.PasswordBannedCharactersLength)]
        [Display(Name = "Banned characters")]
        public string PasswordBannedCharacters { get; set; }

        [Range(Constants.Models.Track.PasswordHistoryMin, Constants.Models.Track.PasswordHistoryMax)]
        [Display(Name = "Password history (number of previous passwords, 0 to disable)")]
        public int PasswordHistory { get; set; }

        [Range(Constants.Models.Track.PasswordMaxAgeMin, Constants.Models.Track.PasswordMaxAgeMax)]
        [Display(Name = "Password max age in seconds (0 to disable)")]
        public long PasswordMaxAge { get; set; }

        [Range(Constants.Models.Track.SoftPasswordChangeMin, Constants.Models.Track.SoftPasswordChangeMax)]
        [Display(Name = "Soft password change in seconds (0 to disable)")]
        public long SoftPasswordChange { get; set; }

        [ListLength(Constants.Models.Track.PasswordPoliciesMin, Constants.Models.Track.PasswordPoliciesMax)]
        [ValidateComplexType]
        [Display(Name = "Password policy groups")]
        public List<PasswordPolicyViewModel> PasswordPolicies { get; set; }

        [ValidateComplexType]
        public ExternalPasswordViewModel ExternalPassword { get; set; } = new ExternalPasswordViewModel();

        [ValidateComplexType]
        [ListLength(Constants.Models.Track.AllowIframeOnDomainsMin, Constants.Models.Track.AllowIframeOnDomainsMax, Constants.Models.Track.AllowIframeOnDomainsLength)]
        [Display(Name = "Allow Iframe on domains (domain without https://)")]
        public List<string> AllowIframeOnDomains { get; set; }
    }
}
