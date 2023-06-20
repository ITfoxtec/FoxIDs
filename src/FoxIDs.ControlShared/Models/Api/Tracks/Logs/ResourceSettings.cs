using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Resource settings in track.
    /// </summary>
    public class ResourceSettings
    {
        [Display(Name = "Show resource ID - to find text resources")]
        public bool ShowResourceId { get; set; }
    }
}
