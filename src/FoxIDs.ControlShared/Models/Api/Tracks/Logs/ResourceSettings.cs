using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Resource settings in environment.
    /// </summary>
    public class ResourceSettings
    {
        [Display(Name = "Show text ID")]
        public bool ShowResourceId { get; set; }
    }
}
