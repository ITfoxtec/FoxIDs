using Newtonsoft.Json;

namespace FoxIDs.Models.SearchModels
{
    public class SearchUpPartyWithProfile<TProfile> : UpPartyWithProfile<TProfile> where TProfile : UpPartyProfile
    {
        [JsonProperty(PropertyName = "client")]
        public SearchOAuthUpClient Client { get; set; }
    }
}
