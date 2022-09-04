using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class ResetPasswordSequenceData : ISequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "e")]
        public string Email { get; set; }

        [Required]
        [JsonProperty(PropertyName = "h")]
        public string PasswordHash { get; set; }
    }
}
