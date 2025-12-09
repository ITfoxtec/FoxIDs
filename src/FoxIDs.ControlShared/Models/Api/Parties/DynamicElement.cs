using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Configurable UI element used to build dynamic login pages.
    /// </summary>
    public class DynamicElement : IValidatableObject
    {
        /// <summary>
        /// Technical name of the element.
        /// </summary>
        [MaxLength(Constants.Models.DynamicElements.NameLength)]
        [RegularExpression(Constants.Models.DynamicElements.NameRegExPattern)]
        [Display(Name = "Technical element name")]
        public string Name { get; set; }

        /// <summary>
        /// Element type defining layout and validation.
        /// </summary>
        [Required]
        public DynamicElementTypes Type { get; set; }

        /// <summary>
        /// Order of the element on the page.
        /// </summary>
        [Required]
        [Range(Constants.Models.DynamicElements.ElementsOrderMin, Constants.Models.DynamicElements.ElementsOrderMax)]
        public int Order { get; set; }

        /// <summary>
        /// Require the field to be completed.
        /// </summary>
        [Display(Name = "Field is required")]
        public bool Required { get; set; }

        /// <summary>
        /// Marks the field as a user identifier.
        /// </summary>
        [Display(Name = "Login user identifier")]
        public bool IsUserIdentifier { get; set; }

        /// <summary>
        /// Content used for text or HTML elements.
        /// </summary>
        [MaxLength(Constants.Models.DynamicElements.ContentLength)]
        [Display(Name = "Text or HTML content")]
        public string Content { get; set; }

        /// <summary>
        /// Display label for the element.
        /// </summary>
        [MaxLength(Constants.Models.DynamicElements.DisplayNameLength)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Maximum allowed input length.
        /// </summary>
        [Display(Name = "Input max length")]
        public int MaxLength { get; set; }

        /// <summary>
        /// Regular expression applied to validate input.
        /// </summary>
        [MaxLength(Constants.Models.DynamicElements.RegExLength)]
        [Display(Name = "Input regex validation")]
        public string RegEx { get; set; }

        /// <summary>
        /// Error message shown when regex validation fails.
        /// </summary>
        [MaxLength(Constants.Models.DynamicElements.ErrorMessageLength)]
        [Display(Name = "RegEx validation error message")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Claim type to map the output value to.
        /// </summary>
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Output claim type")]
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

            if (Type == DynamicElementTypes.Text || Type == DynamicElementTypes.Html || Type == DynamicElementTypes.LargeText || Type == DynamicElementTypes.LargeHtml)
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

            if (Type == DynamicElementTypes.Checkbox)
            {
                if (DisplayName.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field {nameof(DisplayName)} is required for dynamic element type '{Type}'.", [nameof(DisplayName)]));
                }
            }

            return results;
        }
    }
}