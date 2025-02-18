using FoxIDs.Models.Logic;
using FoxIDs.Models.Session;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public abstract class DownSequenceData : IDownSequenceData
    {
        public DownSequenceData()
        { }

        public DownSequenceData(ILoginRequest loginRequest)
        {
            DownPartyLink = loginRequest.DownPartyLink;
            LoginAction = loginRequest.LoginAction;
            UserId = loginRequest.UserId;
            MaxAge = loginRequest.MaxAge;
            LoginHint = loginRequest.LoginHint;
            Acr = loginRequest.Acr;
        }

        [JsonProperty(PropertyName = "dp")]
        public DownPartySessionLink DownPartyLink { get; set; }

        [JsonProperty(PropertyName = "la")]
        public LoginAction LoginAction { get; set; }

        [JsonProperty(PropertyName = "u")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "ma")]
        public int? MaxAge { get; set; }

        [JsonProperty(PropertyName = "lh")]
        public string LoginHint { get; set; }

        [JsonProperty(PropertyName = "a")]
        public IEnumerable<string> Acr { get; set; }
    }
}
