using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class UsageSettings : MasterDocument
    {
        public static Task<string> IdFormatAsync() => IdFormatAsync(new IdKey());

        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.UsageSettings}:{idKey.Master}";
        }
        public static string PartitionIdFormat() => PartitionIdFormat(new IdKey());

        public static new string PartitionIdFormat(IdKey idKey) => $"{idKey.Master}:{Constants.Models.DataType.UsageSettings}";

        [MaxLength(Constants.Models.UsageSettings.IdLength)]
        [RegularExpression(Constants.Models.UsageSettings.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [ListLength(Constants.Models.UsageSettings.CurrencyExchangesMin, Constants.Models.UsageSettings.CurrencyExchangesMax)]
        [JsonProperty(PropertyName = "currency_exchanges")]
        public List<UsageCurrencyExchange> CurrencyExchanges { get; set; }

        /// <summary>
        /// The current / last used invoice number.
        /// </summary>
        [Min(Constants.Models.UsageSettings.InvoiceNumberMin)]
        [JsonProperty(PropertyName = "invoice_number")]
        public int InvoiceNumber { get; set; }

        /// <summary>
        /// Optionally added in front of the invoice number.
        /// </summary>
        [MaxLength(Constants.Models.UsageSettings.InvoiceNumberPrefixLength)]
        [RegularExpression(Constants.Models.UsageSettings.InvoiceNumberPrefixRegExPattern)]
        [JsonProperty(PropertyName = "invoice_number_prefix")]
        public string InvoiceNumberPrefix { get; set; }
    }
}
