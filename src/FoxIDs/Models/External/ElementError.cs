using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ElementError
    {
        [Required]
        [MaxLength(Constants.Models.DynamicElements.NameLength)]
        public string Name { get; set; }

        [MaxLength(Constants.ExternalConnect.UiErrorMessageLength)]
        public string UiErrorMessage { get; set; }
    }
}
