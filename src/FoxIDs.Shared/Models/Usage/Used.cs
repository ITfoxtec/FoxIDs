using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Models.Usage
{
    public class Used : DataDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.Used}:{idKey.TenantName}:{idKey.Year}-{idKey.Month}";
        }

        public static new string PartitionIdFormat(IdKey idKey) => $"{idKey.TenantName}:{Constants.Models.DataType.Used}";

        [Required]
        [MaxLength(Constants.Models.Used.IdLength)]
        [RegularExpression(Constants.Models.Used.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }






        public class IdKey : Tenant.IdKey
        {
            [Required]
            [Min(Constants.Models.Used.YearMin)]
            public int Year { get; set; }

            [Required]
            [Range(Constants.Models.Used.MonthMin, Constants.Models.Used.MonthMax)]
            public int Month { get; set; }
        }
    }
}
