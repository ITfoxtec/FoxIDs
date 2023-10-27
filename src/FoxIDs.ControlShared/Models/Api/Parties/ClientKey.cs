using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ClientKey
    {
        [Required]
        public ClientKeyTypes Type { get; set; }

        [Required]
        public string ExternalName { get; set; }

        [Required]
        public JwtWithCertificateInfo PublicKey { get; set; }
    }
}
