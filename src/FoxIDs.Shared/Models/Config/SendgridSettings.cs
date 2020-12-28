using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class SendgridSettings
    {
        /// <summary>
        /// From email.
        /// </summary>
        [Required]
        public string FromEmail { get; set; }

        /// <summary>
        /// API key.
        /// </summary>
        [Required]
        public string ApiKey { get; set; }
    }
}
