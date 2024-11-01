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

            return $"{Constants.Models.DataType.Used}:{idKey.TenantName}:{idKey.PeriodYear}-{idKey.PeriodMonth}";
        }

        public static async Task<string> IdFormatAsync(string tenantName, int periodYear, int periodMonth)
        {
            if (tenantName == null) new ArgumentNullException(nameof(tenantName));
            if (periodYear <= 0) new ArgumentNullException(nameof(periodYear));
            if (periodMonth <= 0) new ArgumentNullException(nameof(periodMonth));

            return await IdFormatAsync(new IdKey
            {
                TenantName = tenantName,
                PeriodYear = periodYear,
                PeriodMonth = periodMonth
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

        [JsonProperty(PropertyName = "payment_id")]
        public string PaymentId { get; set; }

        [JsonProperty(PropertyName = "tracks")]
        public decimal Tracks { get; set; }

        [JsonProperty(PropertyName = "users")]
        public decimal Users { get; set; }

        [JsonProperty(PropertyName = "logins")]
        public decimal Logins { get; set; }

        [JsonProperty(PropertyName = "token_requests")]
        public decimal TokenRequests { get; set; }

        [JsonProperty(PropertyName = "control_api_gets")]
        public decimal ControlApiGets { get; set; }

        [JsonProperty(PropertyName = "control_api_updates")]
        public decimal ControlApiUpdates { get; set; }

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
            public int PeriodYear { get; set; }

            [Required]
            [Range(Constants.Models.Used.PeriodMonthMin, Constants.Models.Used.PeriodMonthMax)]
            public int PeriodMonth { get; set; }
        }
    }
}
