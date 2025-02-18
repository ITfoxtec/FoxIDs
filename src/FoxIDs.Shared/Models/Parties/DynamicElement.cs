using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class DynamicElement : IValidatableObject
    {
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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!Required && (Type == DynamicElementTypes.EmailAndPassword || Type == DynamicElementTypes.Password))
            {
                results.Add(new ValidationResult($"The field {nameof(Required)} must be true for dynamic element type '{Type}'.", [nameof(Required)]));
            }

            return results;
        }
    }
}
