using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Logic
{
    public class ElementError
    {
        [Required]
        public string Name { get; set; }

        public string UiErrorMessage { get; set; }
    }
}
