using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class ApplicationInsightsGlobalSettings
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
