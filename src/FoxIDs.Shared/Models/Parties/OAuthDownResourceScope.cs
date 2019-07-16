using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthDownResourceScope
    {
        [Required]
        [MaxLength(30)]
        [JsonProperty(PropertyName = "resource")]
        public string Resource { get; set; }

        [Length(0, 100, 30)]
        [JsonProperty(PropertyName = "scopes")]
        public List<string> Scopes { get; set; }
    }
}
