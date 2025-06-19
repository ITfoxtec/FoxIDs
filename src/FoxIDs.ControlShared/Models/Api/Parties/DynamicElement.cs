using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DynamicElement : IValidatableObject
    {
        [MaxLength(Constants.Models.DynamicElements.NameLength)]
        [RegularExpression(Constants.Models.DynamicElements.NameRegExPattern)]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        [Required]
        public DynamicElementTypes Type { get; set; }

        [Required]
        [Range(Constants.Models.DynamicElements.ElementsOrderMin, Constants.Models.DynamicElements.ElementsOrderMax)]
        public int Order { get; set; }

        [Display(Name = "Field is required")]
        public bool Required { get; set; }

        [Display(Name = "Login user identifier")]
        public bool IsUserIdentifier { get; set; }

        [Display(Name = "Text or HTML content")]
        [MaxLength(Constants.Models.DynamicElements.ContentLength)]
        public string Content { get; set; }

        [Display(Name = "Input max length")]
        public int MaxLength { get; set; }

        [Display(Name = "Input regex validation")]
        [MaxLength(Constants.Models.DynamicElements.RegExLength)]
        public string RegEx { get; set; }

        [Display(Name = "Invalid input error message")]
        [MaxLength(Constants.Models.DynamicElements.ErrorMessageLength)]
        public string ErrorMessage { get; set; }

        [Display(Name = "Optionally set a output claim type")]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
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
                    results.Add(new ValidationResult($"The field {nameof(ErrorMessage)} is required in connection with the field {nameof(RegEx)}.", [nameof(RegEx), nameof(ErrorMessage)]));
                }
            }

            return results;
        }
    }
}
