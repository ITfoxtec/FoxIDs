using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DynamicElement : IValidatableObject
    {
        [Required]
        public DynamicElementTypes Type { get; set; }

        [Required]
        [Range(Constants.Models.DynamicElements.ElementsOrderMin, Constants.Models.DynamicElements.ElementsOrderMax)]
        public int Order { get; set; }

        [Display(Name = "Field is required")]
        public bool Required { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!Required && (Type == DynamicElementTypes.EmailAndPassword))
            {
                results.Add(new ValidationResult($"The field {nameof(Required)} must be true for dynamic element type '{Type}'.", new[] { nameof(Required) }));
            }

            return results;
        }
    }
}
