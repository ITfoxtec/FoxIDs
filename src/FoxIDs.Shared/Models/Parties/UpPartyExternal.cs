using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class UpPartyExternal : UpParty
    {
        [ValidateComplexType]
        [JsonProperty(PropertyName = "link_external_user")]
        public LinkExternalUser LinkExternalUser { get; set; }
    }
}
