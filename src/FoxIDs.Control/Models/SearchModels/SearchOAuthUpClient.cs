using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.SearchModels
{
    public class SearchOAuthUpClient
    {
        [MaxLength(Constants.Models.OAuthUpParty.Client.ClientIdLength)]
        [JsonProperty(PropertyName = "sp_client_id")]
        public string SpClientId { get; set; }
    }
}
