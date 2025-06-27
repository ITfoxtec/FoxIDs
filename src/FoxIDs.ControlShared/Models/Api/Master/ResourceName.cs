using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ResourceName
    {
        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [Display(Name = "Text look up key (en)")]
        public string Name { get; set; }

        [Required]
        public int Id { get; set; }
    }
}
