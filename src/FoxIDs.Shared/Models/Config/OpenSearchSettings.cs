using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class OpenSearchSettings : OpenSearchBaseSettings
    {
        /// <summary>
        /// Default log lifetime.
        /// </summary>
        [Required]
        public LogLifetimeOptions LogLifetime { get; set; } = LogLifetimeOptions.Max180Days;

        /// <summary>
        /// Default log Name.
        /// </summary>
        [Required]
        public string LogName { get; set; } = Constants.Logs.LogName;

        /// <summary>
        /// Allow insecure certificates.
        /// </summary>
        public bool AllowInsecureCertificates { get; set; }
    }
}
