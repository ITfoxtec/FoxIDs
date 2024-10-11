using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
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

        [Required]
        [Min(Constants.Models.Used.YearMin)]
        [JsonProperty(PropertyName = "year")]
        public int Year { get; set; }

        [Required]
        [Range(Constants.Models.Used.MonthMin, Constants.Models.Used.MonthMax)]
        [JsonProperty(PropertyName = "month")]
        public int Month { get; set; }

        [JsonProperty(PropertyName = "tracks")]
        public double Tracks { get; set; }

        [JsonProperty(PropertyName = "users")]
        public double Users { get; set; }

        [JsonProperty(PropertyName = "logins")]
        public double Logins { get; set; }

        [JsonProperty(PropertyName = "token_requests")]
        public double TokenRequests { get; set; }

        [JsonProperty(PropertyName = "control_api_gets")]
        public double ControlApiGets { get; set; }

        [JsonProperty(PropertyName = "control_api_updates")]
        public double ControlApiUpdates { get; set; }

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
