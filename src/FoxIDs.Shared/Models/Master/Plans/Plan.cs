﻿using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class Plan : MasterDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.Plan}:{idKey.Master}:{idKey.PlanName}";
        }

        public static async Task<string> IdFormatAsync(string planName)
        {
            if (planName == null) new ArgumentNullException(nameof(planName));

            var idKey = new IdKey
            {
                PlanName = planName
            };

            return await IdFormatAsync(idKey);
        }

        public static new string PartitionIdFormat(MasterDocument.IdKey idKey) => $"{idKey.Master}:plan";

        [MaxLength(Constants.Models.Plan.IdLength)]
        [RegularExpression(Constants.Models.Plan.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.Plan.NameLength)]
        [RegularExpression(Constants.Models.Plan.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Plan.DisplayNameLength)]
        [RegularExpression(Constants.Models.Plan.DisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Plan.TextLength)]
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [Required]
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [JsonProperty(PropertyName = "cost_per_month")]
        public decimal CostPerMonth { get; set; }

        [JsonProperty(PropertyName = "enable_custom_domain")]
        public bool EnableCustomDomain { get; set; }

        [JsonProperty(PropertyName = "enable_sms")]
        public bool EnableSms { get; set; }

        [JsonProperty(PropertyName = "enable_email_two_factor")]
        public bool EnableEmailTwoFactor { get; set; }

        [Required]
        [JsonProperty(PropertyName = "tracks")]
        public PlanItem Tracks { get; set; } = new PlanItem();

        [Required]
        [JsonProperty(PropertyName = "users")]
        public PlanItem Users { get; set; }

        [Required]
        [JsonProperty(PropertyName = "logins")]
        public PlanItem Logins { get; set; }

        [Required]
        [JsonProperty(PropertyName = "token_req")]
        public PlanItem TokenRequests { get; set; }

        [Required]
        [JsonProperty(PropertyName = "sms")]
        public PlanItem Sms { get; set; } = new PlanItem();

        [Required]
        [JsonProperty(PropertyName = "emails")]
        public PlanItem Emails { get; set; } = new PlanItem();

        [JsonProperty(PropertyName = "log_lifetime")]
        public LogLifetimeOptions? LogLifetime { get; set; }

        public new class IdKey : MasterDocument.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Plan.NameLength)]
            [RegularExpression(Constants.Models.Plan.NameRegExPattern)]
            public string PlanName { get; set; }
        }
    }
}
