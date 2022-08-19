using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class UpParty : Party
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"party:up:{idKey.TenantName}:{idKey.TrackName}:{idKey.PartyName}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, string name)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (name == null) new ArgumentNullException(nameof(name));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                PartyName = name,
            };

            return await IdFormatAsync(idKey);
        }

        [JsonProperty(PropertyName = "party_binding_pattern")]
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        [Range(Constants.Models.UpParty.SessionLifetimeMin, Constants.Models.UpParty.SessionLifetimeMax)]
        [JsonProperty(PropertyName = "session_lifetime")]
        public int SessionLifetime { get; set; } = 36000;

        [Range(Constants.Models.UpParty.SessionAbsoluteLifetimeMin, Constants.Models.UpParty.SessionAbsoluteLifetimeMax)]
        [JsonProperty(PropertyName = "session_absolute_lifetime")]
        public int SessionAbsoluteLifetime { get; set; } = 86400;

        [Range(Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMin, Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMax)]
        [JsonProperty(PropertyName = "persistent_session_absolute_lifetime")]
        public int PersistentSessionAbsoluteLifetime { get; set; }

        [JsonProperty(PropertyName = "persistent_session_lifetime_unlimited")]
        public bool PersistentSessionLifetimeUnlimited { get; set; }

        [JsonProperty(PropertyName = "disable_single_logout")]
        public bool DisableSingleLogout { get; set; }

        [Length(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [JsonProperty(PropertyName = "hrd_domains")]
        public List<string> HrdDomains { get; set; }

        [MaxLength(Constants.Models.UpParty.HrdDisplayNameLength)]
        [RegularExpression(Constants.Models.UpParty.HrdDisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "hrd_display_name")]
        public string HrdDisplayName { get; set; }

        [MaxLength(Constants.Models.UpParty.HrdLogoUrlLength)]
        [RegularExpression(Constants.Models.UpParty.HrdLogoUrlRegExPattern)]
        [JsonProperty(PropertyName = "hrd_logo_url")]
        public string HrdLogoUrl { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
        }
    }
}