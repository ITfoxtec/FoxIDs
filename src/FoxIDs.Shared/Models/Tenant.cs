using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class Tenant : DataDocument
    {
        public static async Task<string> IdFormat(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"tenant:{idKey.TenantName}";
        }
        public static string PartitionIdFormat(string tenantName) => tenantName;

        [Required]
        [MaxLength(Constants.Models.TenantIdLength)]
        [RegularExpression(Constants.Models.TenantIdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [JsonIgnore]
        public string Name
        {
            get
            {
                return Id.Substring(Id.LastIndexOf(':') + 1);
            }
        }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
        }

        public class IdKey
        {
            [Required]
            [MaxLength(Constants.Models.TenantNameLength)]
            [RegularExpression(Constants.Models.TenantNameRegExPattern)]
            public string TenantName { get; set; }
        }
    }
}
