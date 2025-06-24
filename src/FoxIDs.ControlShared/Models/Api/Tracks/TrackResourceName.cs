using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackResourceName
    {
        [Required]
        [MaxLength(Constants.Models.Resource.NameLength)]
        [Display(Name = "Text look up key (en)")]
        public string Name { get; set; }

        public int Id { get; set; }
    }
}
