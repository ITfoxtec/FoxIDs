using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class Track : DataDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.Track}:{idKey.TenantName}:{idKey.TrackName}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, string name)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (name == null) new ArgumentNullException(nameof(name));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = name,
            };

            return await IdFormatAsync(idKey);
        }

        public static new string PartitionIdFormat(IdKey idKey) => $"{idKey.TenantName}:tracks";

        [Required]
        [MaxLength(Constants.Models.Track.IdLength)]
        [RegularExpression(Constants.Models.Track.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "key")]
        public TrackKey Key { get; set; }

        [Range(Constants.Models.Track.KeyValidityInMonthsMin, Constants.Models.Track.KeyValidityInMonthsMax)]
        [JsonProperty(PropertyName = "key_validity_in_months")]
        public int KeyValidityInMonths { get; set; } = 3;

        [Range(Constants.Models.Track.KeyAutoRenewDaysBeforeExpiryMin, Constants.Models.Track.KeyAutoRenewDaysBeforeExpiryMax)]
        [JsonProperty(PropertyName = "key_auto_renew_days_before_expiry")]
        public int KeyAutoRenewDaysBeforeExpiry { get; set; } = 10;

        [Range(Constants.Models.Track.KeyPrimaryAfterDaysMin, Constants.Models.Track.KeyPrimaryAfterDaysMax)]
        [JsonProperty(PropertyName = "key_primary_after_days")]
        public int KeyPrimaryAfterDays { get; set; } = 5;

        [Range(Constants.Models.Track.KeyExternalCacheLifetimeMin, Constants.Models.Track.KeyExternalCacheLifetimeMax)]
        [JsonProperty(PropertyName = "key_external_cache_lifetime")]
        public int KeyExternalCacheLifetime { get; set; } = 28800; // 8 hours

        [ListLength(Constants.Models.Claim.MapMin, Constants.Models.Claim.MapMax)]
        [JsonProperty(PropertyName = "claim_mappings")]
        public List<ClaimMap> ClaimMappings { get; set; }

        [JsonProperty(PropertyName = "auto_map_saml_claims")]
        public bool AutoMapSamlClaims { get; set; }

        [ListLength(Constants.Models.Track.ResourcesMin, Constants.Models.Track.ResourcesMax)]
        [JsonProperty(PropertyName = "resources")]
        public List<ResourceItem> Resources { get; set; }

        [JsonProperty(PropertyName = "show_resource_id")]
        public bool ShowResourceId { get; set; }

        [Range(Constants.Models.Track.SequenceLifetimeMin, Constants.Models.Track.SequenceLifetimeMax)] // 30 seconds to 5 hours. Default 2 hours.
        [JsonProperty(PropertyName = "sequence_lifetime")] 
        public int SequenceLifetime { get; set; } = Constants.TrackDefaults.DefaultSequenceLifetime;

        [Range(Constants.Models.Track.MaxFailingLoginsMin, Constants.Models.Track.MaxFailingLoginsMax)] 
        [JsonProperty(PropertyName = "max_failing_logins")]
        public int MaxFailingLogins { get; set; } = Constants.TrackDefaults.DefaultMaxFailingLogins;

        [Range(Constants.Models.Track.FailingLoginCountLifetimeMin, Constants.Models.Track.FailingLoginCountLifetimeMax)]
        [JsonProperty(PropertyName = "failing_login_count_lifetime")]
        public int FailingLoginCountLifetime { get; set; } = Constants.TrackDefaults.DefaultFailingLoginCountLifetime;

        [Range(Constants.Models.Track.FailingLoginObservationPeriodMin, Constants.Models.Track.FailingLoginObservationPeriodMax)]
        [JsonProperty(PropertyName = "failing_login_observation_period")]
        public int FailingLoginObservationPeriod { get; set; } = Constants.TrackDefaults.DefaultFailingLoginObservationPeriod;

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [JsonProperty(PropertyName = "password_length")]
        public int PasswordLength { get; set; } = Constants.TrackDefaults.DefaultPasswordLength;

        [Required]
        [JsonProperty(PropertyName = "check_password_complexity")]
        public bool? CheckPasswordComplexity { get; set; } = true;

        [Required]
        [JsonProperty(PropertyName = "check_password_risk")]
        public bool? CheckPasswordRisk { get; set; } = true;

        [ListLength(Constants.Models.Track.AllowIframeOnDomainsMin, Constants.Models.Track.AllowIframeOnDomainsMax, Constants.Models.Track.AllowIframeOnDomainsLength)]
        [JsonProperty(PropertyName = "allow_iframe_on_domains")]
        public List<string> AllowIframeOnDomains { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "send_email")]
        public SendEmail SendEmail { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "logging")]
        public Logging Logging { get; set; } 
        
        [MaxLength(Constants.Models.Track.DisplayNameLength)]
        [RegularExpression(Constants.Models.Track.DisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Address.NameLength)]
        [JsonProperty(PropertyName = "company_name")]
        public string CompanyName { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine1Length)]
        [JsonProperty(PropertyName = "address_line_1")]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine2Length)]
        [JsonProperty(PropertyName = "address_line_2")]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Address.PostalCodeLength)]
        [JsonProperty(PropertyName = "postal_code")]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Address.CityLength)]
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [MaxLength(Constants.Models.Address.StateRegionLength)]
        [JsonProperty(PropertyName = "state_region")]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Address.CountryLength)]
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
            Name = Id.Substring(Id.LastIndexOf(':') + 1); ;
        }

        public class IdKey : Tenant.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Track.NameLength)]
            [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
            public string TrackName { get; set; }
        }
    }
}
