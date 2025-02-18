using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Sequences
{
    public class TrackLinkUpSequenceData : UpSequenceData, ISequenceKey
    {
        public TrackLinkUpSequenceData() : base() { }

        public TrackLinkUpSequenceData(ILoginRequest loginRequest) : base(loginRequest) { }

        [JsonIgnore]
        public string KeyName
        {
            get
            {
                if (KeyNames.Count() != 1)
                {
                    throw new Exception("KeyNames do not contain exactly one element.");
                }
                return KeyNames.First();
            }
            set
            {
                KeyNames = new List<string> { value };
            }
        }

        [Required]
        [JsonProperty(PropertyName = "kn2")]
        public List<string> KeyNames { get; set; }

        [JsonProperty(PropertyName = "kvu")]
        public long KeyValidUntil { get; set; }

        [JsonProperty(PropertyName = "ku")]
        public bool KeyUsed { get; set; }

        [JsonProperty(PropertyName = "dss")]
        public string DownPartySequenceString { get; set; }

        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public bool RequireLogoutConsent { get; set; }
    }
}
