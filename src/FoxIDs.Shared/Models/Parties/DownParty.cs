using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    /// <summary>
    /// Application registration.
    /// </summary>
    public class DownParty : Party
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.DownParty}:{idKey.TenantName}:{idKey.TrackName}:{idKey.PartyName}";
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

        [ListLength(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax)]
        [JsonProperty(PropertyName = "allow_up_parties")]
        public List<UpPartyLink> AllowUpParties { get; set; }

        [JsonProperty(PropertyName = "restrict_form_action")]
        public bool RestrictFormAction { get; set; }

        #region TestApp
        [JsonProperty(PropertyName = "is_test")]
        public bool? IsTest { get; set; }

        [MaxLength(Constants.Models.DownParty.UrlLengthMax)]
        [JsonProperty(PropertyName = "test_url")]
        public string TestUrl { get; set; }

        [JsonProperty(PropertyName = "test_expire_at")]
        public long? TestExpireAt { get; set; }

        /// <summary>
        /// 0 to disable expiration.
        /// </summary>
        [JsonProperty(PropertyName = "test_expire_in_seconds")]
        public int? TestExpireInSeconds { get; set; }
        #endregion

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
        }

        public async Task SetIdAsync(RouteBinding routeBinding, string name)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (name == null) new ArgumentNullException(nameof(name));

            Id = await IdFormatAsync(routeBinding, name);
        }
    }
}