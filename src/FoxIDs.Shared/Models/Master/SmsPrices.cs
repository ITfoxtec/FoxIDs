using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class SmsPrices : MasterDocument
    {
        public static Task<string> IdFormatAsync() => IdFormatAsync(new IdKey());
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.SmsPrices}:{idKey.Master}";
        }
        public static string PartitionIdFormat() => PartitionIdFormat(new IdKey());

        public static new string PartitionIdFormat(IdKey idKey) => $"{idKey.Master}:{Constants.Models.DataType.SmsPrices}";

        [MaxLength(Constants.Models.SmsPrices.IdLength)]
        [RegularExpression(Constants.Models.SmsPrices.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [ListLength(Constants.Models.SmsPrices.CountriesMin, Constants.Models.SmsPrices.CountriesMax)]
        [JsonProperty(PropertyName = "countries")]
        public List<SmsPrice> Countries { get; set; }
    }
}
