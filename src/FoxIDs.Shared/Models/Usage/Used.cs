using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class Used : DataDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.Used}:{idKey.TenantName}:{idKey.Year}-{idKey.Month}";
        }

        public static async Task<string> IdFormatAsync(string tenantName, int year, int month)
        {
            if (tenantName == null) new ArgumentNullException(nameof(tenantName));
            if (year <= 0) new ArgumentNullException(nameof(year));
            if (month <= 0) new ArgumentNullException(nameof(month));

            return await IdFormatAsync(new IdKey
            {
                TenantName = tenantName,
                Year = year,
                Month = month
            });
        }

        public static string PartitionIdFormat() => Constants.Models.DataType.Used;

        [Required]
        [MaxLength(Constants.Models.Used.IdLength)]
        [RegularExpression(Constants.Models.Used.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        [JsonProperty(PropertyName = "tenant_name")]
        public string TenantName { get; set; }

        [Required]
        [Min(Constants.Models.Used.PeriodYearMin)]
        [JsonProperty(PropertyName = "period_year")]
        public int PeriodYear { get; set; }

        [Required]
        [Range(Constants.Models.Used.PeriodMonthMin, Constants.Models.Used.PeriodMonthMax)]
        [JsonProperty(PropertyName = "period_month")]
        public int PeriodMonth { get; set; }

        [JsonProperty(PropertyName = "invoice_status")]
        public UsedInvoiceStatus InvoiceStatus { get; set; }

        [JsonProperty(PropertyName = "payment_status")]
        public UsedPaymentStatus PaymentStatus { get; set; }

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

        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        [JsonProperty(PropertyName = "items")]
        public List<UsedItem> Items { get; set; }

        [ListLength(Constants.Models.Used.InvoicesMin, Constants.Models.Used.InvoicesMax)]
        [JsonProperty(PropertyName = "invoices")]
        public List<Invoice> Invoices { get; set; }

        public class IdKey : Tenant.IdKey
        {
            [Required]
            [Min(Constants.Models.Used.PeriodYearMin)]
            public int Year { get; set; }

            [Required]
            [Range(Constants.Models.Used.PeriodMonthMin, Constants.Models.Used.PeriodMonthMax)]
            public int Month { get; set; }
        }
    }
}
