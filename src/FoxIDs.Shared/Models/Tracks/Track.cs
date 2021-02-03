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
        public static async Task<string> IdFormat(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"track:{idKey.TenantName}:{idKey.TrackName}";
        }

        public static async Task<string> IdFormat(RouteBinding routeBinding, string name)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (name == null) new ArgumentNullException(nameof(name));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = name,
            };

            return await IdFormat(idKey);
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

        [Range(Constants.Models.Track.KeyExternalValidityInMonthsMin, Constants.Models.Track.KeyExternalValidityInMonthsMax)]
        public int KeyExternalValidityInMonths { get; set; } = 3;

        [Range(Constants.Models.Track.KeyExternalAutoRenewDaysBeforeExpiryMin, Constants.Models.Track.KeyExternalAutoRenewDaysBeforeExpiryMax)]
        public int KeyExternalAutoRenewDaysBeforeExpiry { get; set; } = 10;

        [Range(Constants.Models.Track.KeyExternalPrimaryAfterDaysMin, Constants.Models.Track.KeyExternalPrimaryAfterDaysMax)]
        public int KeyExternalPrimaryAfterDays { get; set; } = 5;

        [Range(Constants.Models.Track.KeyExternalCacheLifetimeMin, Constants.Models.Track.KeyExternalCacheLifetimeMax)]
        public int KeyExternalCacheLifetime { get; set; } = 28800; // 8 hours

        [Length(Constants.Models.Claim.MapMin, Constants.Models.Claim.MapMax)]
        [JsonProperty(PropertyName = "claim_mappings")]
        public List<ClaimMap> ClaimMappings { get; set; }

        [Length(Constants.Models.Track.ResourcesMin, Constants.Models.Track.ResourcesMax)]
        [JsonProperty(PropertyName = "resources")]
        public List<ResourceItem> Resources { get; set; }

        [Range(Constants.Models.Track.SequenceLifetimeMin, Constants.Models.Track.SequenceLifetimeMax)] // 30 seconds to 3 hours
        [JsonProperty(PropertyName = "sequence_lifetime")] 
        public int SequenceLifetime { get; set; }

        [Range(Constants.Models.Track.MaxFailingLoginsMin, Constants.Models.Track.MaxFailingLoginsMax)] 
        [JsonProperty(PropertyName = "max_failing_logins")]
        public int MaxFailingLogins { get; set; }

        [Range(Constants.Models.Track.FailingLoginCountLifetimeMin, Constants.Models.Track.FailingLoginCountLifetimeMax)]
        [JsonProperty(PropertyName = "failing_login_count_lifetime")]
        public int FailingLoginCountLifetime { get; set; }

        [Range(Constants.Models.Track.FailingLoginObservationPeriodMin, Constants.Models.Track.FailingLoginObservationPeriodMax)]
        [JsonProperty(PropertyName = "failing_login_observation_period")]
        public int FailingLoginObservationPeriod { get; set; }

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [JsonProperty(PropertyName = "password_length")]
        public int PasswordLength { get; set; }

        [Required]
        [JsonProperty(PropertyName = "check_password_complexity")]
        public bool? CheckPasswordComplexity { get; set; }

        [Required]
        [JsonProperty(PropertyName = "check_password_risk")]
        public bool? CheckPasswordRisk { get; set; }

        [Length(Constants.Models.Track.AllowIframeOnDomainsMin, Constants.Models.Track.AllowIframeOnDomainsMax, Constants.Models.Track.AllowIframeOnDomainsLength)]
        [JsonProperty(PropertyName = "allow_iframe_on_domains")]
        public List<string> AllowIframeOnDomains { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "send_email")]
        public SendEmail SendEmail { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
            Name = Id.Substring(Id.LastIndexOf(':') + 1); ;
        }

        public class IdKey : Tenant.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Track.NameLength)]
            [RegularExpression(Constants.Models.Track.NameRegExPattern)]
            public string TrackName { get; set; }
        }
    }
}
