using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class ApplicationInsightsSettings
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
