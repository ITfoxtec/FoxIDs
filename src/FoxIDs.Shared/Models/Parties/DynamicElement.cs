using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class DynamicElement : IValidatableObject
    {
        [MaxLength(Constants.Models.DynamicElements.NameLength)]
        [RegularExpression(Constants.Models.DynamicElements.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Required]
        [JsonProperty(PropertyName = "type")]
        public DynamicElementTypes Type { get; set; }

        [Required]
        [Range(Constants.Models.DynamicElements.ElementsOrderMin, Constants.Models.DynamicElements.ElementsOrderMax)]
        [JsonProperty(PropertyName = "order")]
        public int Order { get; set; }     

        [JsonProperty(PropertyName = "required")]
        public bool Required { get; set; }

        [JsonProperty(PropertyName = "is_user_identifier")]
        public bool IsUserIdentifier { get; set; }

        [MaxLength(Constants.Models.DynamicElements.ContentLength)]
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        [MaxLength(Constants.Models.DynamicElements.DisplayNameLength)]
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [Max(Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "max_length")]
        public int MaxLength { get; set; }

        [MaxLength(Constants.Models.DynamicElements.RegExLength)]
        [JsonProperty(PropertyName = "regex")]
        public string RegEx { get; set; }

        [MaxLength(Constants.Models.DynamicElements.ErrorMessageLength)]
        [JsonProperty(PropertyName = "error_message")]
        public string ErrorMessage { get; set; }

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [JsonProperty(PropertyName = "claim_out")]
        public string ClaimOut { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!Required && (Type == DynamicElementTypes.EmailAndPassword || Type == DynamicElementTypes.Password))
            {
                results.Add(new ValidationResult($"The field {nameof(Required)} must be true for dynamic element type '{Type}'.", [nameof(Required)]));
            }

            if (IsUserIdentifier && !(Type == DynamicElementTypes.EmailAndPassword || Type == DynamicElementTypes.Email || Type == DynamicElementTypes.Phone || Type == DynamicElementTypes.Username))
            {
                results.Add(new ValidationResult($"The field {nameof(Type)}='{Type}' can not be a user identifier'.", [nameof(Type), nameof(IsUserIdentifier)]));
            }

            if (Type == DynamicElementTypes.Text || Type == DynamicElementTypes.Html)
            {
                if (Content.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field {nameof(Content)} must not be empty for dynamic element type '{Type}'.", [nameof(Content)]));
                }
            }

            if (Type == DynamicElementTypes.Custom)
            {
                if (DisplayName.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field {nameof(DisplayName)} is required for dynamic element type '{Type}'.", [nameof(DisplayName)]));
                }

                if (MaxLength > Constants.Models.Claim.LimitedValueLength)
                {
                    results.Add(new ValidationResult($"The field {nameof(MaxLength)} must not exceed '{Constants.Models.Claim.LimitedValueLength}'.", [nameof(MaxLength)]));
                }
                if (MaxLength < 1)
                {
                    results.Add(new ValidationResult($"The field {nameof(MaxLength)} must be at least '1'.", [nameof(MaxLength)]));
                }

                if (!RegEx.IsNullOrWhiteSpace() && ErrorMessage.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field {nameof(ErrorMessage)} is required in connection with the field {nameof(RegEx)}.", [nameof(ErrorMessage)]));
                }
            }

            return results;
        }
    }
}