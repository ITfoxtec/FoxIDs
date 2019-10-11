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
        [MaxLength(50)]
        [RegularExpression(@"^[\w:_-]*$")]
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
            [MaxLength(30)]
            [RegularExpression(@"^\w[\w-_]*$")]
            public string TenantName { get; set; }
        }
    }
}
