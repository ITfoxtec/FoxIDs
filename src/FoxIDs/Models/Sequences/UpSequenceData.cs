using FoxIDs.Models.Logic;
using FoxIDs.Models.Session;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public abstract class UpSequenceData : IUpSequenceData, IValidatableObject
    {
        protected UpSequenceData()
        { }

        public UpSequenceData(ILoginRequest loginRequest) 
        {
            DownPartyLink = loginRequest.DownPartyLink;
            LoginAction = loginRequest.LoginAction;
            UserId = loginRequest.UserId;
            MaxAge = loginRequest.MaxAge;
            LoginHint = loginRequest.LoginHint;
            Acr = loginRequest.Acr;
        }

        [JsonProperty(PropertyName = "es")]
        public bool ExternalInitiatedSingleLogout { get; set; } = false;

        [JsonProperty(PropertyName = "ss")]
        public bool IsSingleLogout { get; set; }        

        [JsonProperty(PropertyName = "dp")]
        public DownPartySessionLink DownPartyLink { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "hln")]
        public string HrdLoginUpPartyName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ui")]
        public string UpPartyId { get; set; }

        [JsonProperty(PropertyName = "pn")]
        public string UpPartyProfileName { get; set; }

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

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!IsSingleLogout && !ExternalInitiatedSingleLogout)
            {
                if (DownPartyLink == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(DownPartyLink)} is required if not internal or external initiated single logout.", [nameof(DownPartyLink)]));
                }
            }

            return results;
        }
    }
}
