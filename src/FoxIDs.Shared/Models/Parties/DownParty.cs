using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    /// <summary>
    /// Down-party.
    /// </summary>
    public class DownParty : Party
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"party:down:{idKey.TenantName}:{idKey.TrackName}:{idKey.PartyName}";
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

        [Length(Constants.Models.DownParty.AllowUpPartyNamesMin, Constants.Models.DownParty.AllowUpPartyNamesMax)]
        [JsonProperty(PropertyName = "allow_up_parties")]
        public List<UpPartyLink> AllowUpParties { get; set; }

        [JsonProperty(PropertyName = "restrict_form_action")]
        public bool RestrictFormAction { get; set; }        

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