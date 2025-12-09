using System;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Metadata describing an X.509 certificate.
    /// </summary>
    public class CertificateInfo
    {
        /// <summary>
        /// Certificate subject distinguished name.
        /// </summary>
        public string Subject { get; set; }
        /// <summary>
        /// Not before timestamp.
        /// </summary>
        public DateTime ValidFrom { get; set; }
        /// <summary>
        /// Not after timestamp.
        /// </summary>
        public DateTime ValidTo { get; set; }
        /// <summary>
        /// SHA thumbprint of the certificate.
        /// </summary>
        public string Thumbprint { get; set; }
    }
}
