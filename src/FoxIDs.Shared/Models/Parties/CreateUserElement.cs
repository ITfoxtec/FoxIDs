using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class CreateUserElement : IValidatableObject
    {
        [Required]
        public CreateUserElementTypes Type { get; set; }

        [Required]
        [Range(Constants.Models.CreateUser.ElementsOrderMin, Constants.Models.CreateUser.ElementsOrderMax)]
        [JsonProperty(PropertyName = "order")]
        public int Order { get; set; }     

        [JsonProperty(PropertyName = "required")]
        public bool Required { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!Required && (Type == CreateUserElementTypes.EmailAndPassword))
            {
                results.Add(new ValidationResult($"The field {nameof(Required)} must be true for dynamic element type '{Type}'.", new[] { nameof(Required) }));
            }

            return results;
        }
    }
}
