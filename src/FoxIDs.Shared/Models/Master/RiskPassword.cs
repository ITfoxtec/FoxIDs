using FoxIDs.Infrastructure.DataAnnotations;
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

        public static new string PartitionIdFormat(MasterDocument.IdKey idKey) => $"{idKey.Master}:prisk";

        [MaxLength(Constants.Models.RiskPassword.IdLength)]
        [RegularExpression(Constants.Models.RiskPassword.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [Min(Constants.Models.RiskPassword.CountMin)]
        [JsonProperty(PropertyName = "count")]
        public long Count { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; }

        public new class IdKey : MasterDocument.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.RiskPassword.PasswordSha1HashLength)]
            [RegularExpression(Constants.Models.RiskPassword.PasswordSha1HashRegExPattern)]
            public string PasswordSha1Hash { get; set; }
        }
    }
}
