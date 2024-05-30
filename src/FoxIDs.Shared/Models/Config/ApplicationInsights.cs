using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class ApplicationInsights
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
