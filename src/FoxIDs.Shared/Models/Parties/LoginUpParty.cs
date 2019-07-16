using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class LoginUpParty : UpParty
    {
        public LoginUpParty()
        {
            Type = PartyType.Login.ToString();
        }

        [Range(0, 21600)] // 0 minutes to 6 hours
        [JsonProperty(PropertyName = "session_lifetime")]
        public int SessionLifetime { get; set; }

        [Range(0, 86400)] // 0 minutes to 24 hours
        [JsonProperty(PropertyName = "session_absolute_lifetime")]
        public int SessionAbsoluteLifetime { get; set; }

        [Range(0, 31536000)] // 0 min to 12 month
        [JsonProperty(PropertyName = "persistent_session_absolute_lifetime")]
        public int PersistentAbsoluteSessionLifetime { get; set; }

        [Required]
        [JsonProperty(PropertyName = "persistent_session_lifetime_unlimited")]
        public bool? PersistentSessionLifetimeUnlimited { get; set; }

        [Required]
        [JsonProperty(PropertyName = "enable_cancel_login")]
        public bool? EnableCancelLogin { get; set; }

        [Required]
        [JsonProperty(PropertyName = "enable_create_user")]
        public bool? EnableCreateUser { get; set; }        

        [Length(0, 40, 200)]
        [JsonProperty(PropertyName = "allow_iframe_on_domains")]
        public List<string> AllowIframeOnDomains { get; set; }

        [Required]
        [JsonProperty(PropertyName = "logout_consent")]
        public string LogoutConsent { get; set; }
    }
}
