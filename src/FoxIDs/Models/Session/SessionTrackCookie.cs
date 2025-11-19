using FoxIDs.Infrastructure.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Session
{
    public class SessionTrackCookie : CookieMessage
    {
        [JsonIgnore]
        public override SameSiteMode SameSite => SameSiteMode.None;

        [ListLength(Constants.Models.Session.GroupsMin, Constants.Models.Session.GroupsMax)]
        [JsonProperty(PropertyName = "g")]
        public List<SessionTrackCookieGroup> Groups { get; set; }  
    }
}
