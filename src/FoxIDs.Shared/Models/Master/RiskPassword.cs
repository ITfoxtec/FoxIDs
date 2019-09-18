using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class RiskPassword : MasterDocument
    {
        public static async Task<string> IdFormat(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"prisk:{idKey.Master}:{idKey.PasswordSha1Hash}";
        }

        [MaxLength(70)]
        [RegularExpression(@"^[\w@:_-]*$")]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "count")]
        public long Count { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; }

        public new class IdKey : MasterDocument.IdKey
        {
            [Required]
            [MaxLength(40)]
            [RegularExpression(@"^[A-F0-9]*$")]
            public string PasswordSha1Hash { get; set; }
        }
    }
}
