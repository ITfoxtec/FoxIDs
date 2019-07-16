using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class DownParty : Party
    {
        public static string IdFormat(IdKey idKey) => $"party:down:{idKey.TenantName}:{idKey.TrackName}:{idKey.PartyName}";

        [Length(0, 2000)]
        [JsonProperty(PropertyName = "allow_up_parties")]
        public List<PartyDataElement> AllowUpParties { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            Id = IdFormat(idKey);
        }
    }
}