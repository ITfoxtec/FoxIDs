using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace FoxIDs.Models.Session
{
    public class SessionLoginUpPartyCookie : SessionBaseCookie
    {
        [JsonIgnore]
        public override SameSiteMode SameSite => SameSiteMode.Lax;

        [JsonProperty(PropertyName = "ui")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "e")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "p")]
        public string Phone { get; set; }

        [JsonProperty(PropertyName = "un")]
        public string Username { get; set; }

        /// <summary>
        /// User logged in with the user identifier which is equal to either the EmailIdentifier, PhoneIdentifier or UsernameIdentifier.
        /// </summary>
        [JsonProperty(PropertyName = "uin")]
        public string UserIdentifier { get; set; }
    }
}
