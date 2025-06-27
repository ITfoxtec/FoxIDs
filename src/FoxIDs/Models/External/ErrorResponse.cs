using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ErrorResponse
    {
        [Required]
        [MaxLength(Constants.ExternalConnect.ErrorLength)]
        public string Error { get; set; }

        [MaxLength(Constants.ExternalConnect.ErrorMessageLength)]
        public string ErrorMessage { get; set; }

        [Obsolete("Use ErrorMessage instead.")]
        [MaxLength(Constants.ExternalConnect.ErrorMessageLength)]
        public string ErrorDescription { get; set; }

        [MaxLength(Constants.ExternalConnect.UiErrorMessageLength)]
        public string UiErrorMessage { get; set; }

        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        public IEnumerable<ElementError> Elements { get; set; }
    }
}
