using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ResourceName
    {
        [Required]
        [MaxLength(500)]
        public string Name { get; set; }

        [Required]
        public int Id { get; set; }
    }
}
