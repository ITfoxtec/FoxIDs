using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Sequences
{
    public class TrackLinkDownSequenceData : ISequenceKey
    {
        [JsonIgnore]
        public string KeyName 
        {
            get 
            { 
                if(KeyNames.Count() != 1)
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
        [JsonProperty(PropertyName = "kns")]
        public List<string> KeyNames { get; set; }

        [JsonProperty(PropertyName = "kvu")]
        public long KeyValidUntil { get; set; }

        [JsonProperty(PropertyName = "ku")]
        public bool KeyUsed { get; set; }

        [JsonProperty(PropertyName = "uss")]
        public string UpPartySequenceString { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "e")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "ed")]
        public string ErrorDescription { get; set; }
 
    }
}
