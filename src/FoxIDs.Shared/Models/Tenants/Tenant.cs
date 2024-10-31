﻿using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class Tenant : DataDocument, IValidatableObject
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.Tenant}:{idKey.TenantName}";
        }

        public static async Task<string> IdFormatAsync(string name)
        {
            if (name == null) new ArgumentNullException(nameof(name));

            return await IdFormatAsync(new IdKey
            {
                TenantName = name,
            });
        }

        public static string PartitionIdFormat() => "tenants";

        [Required]
        [MaxLength(Constants.Models.Tenant.IdLength)]
        [RegularExpression(Constants.Models.Tenant.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; }

        [MaxLength(Constants.Models.Plan.NameLength)]
        [RegularExpression(Constants.Models.Plan.NameRegExPattern)]
        [JsonProperty(PropertyName = "plan_name")]
        public string PlanName { get; set; }

        /// <summary>
        /// Default EUR if empty.
        /// </summary>
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }

        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [RegularExpression(Constants.Models.Tenant.CustomDomainRegExPattern, ErrorMessage = "The field {0} must be a valid domain.")]
        [JsonProperty(PropertyName = "custom_domain")]
        public string CustomDomain { get; set; }

        [JsonProperty(PropertyName = "custom_domain_verified")]
        public bool CustomDomainVerified { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "customer")]
        public Customer Customer { get; set; }

        [ValidateComplexType]
        [JsonProperty(PropertyName = "payment")]
        public Payment Payment { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
            Name = Id.Substring(Id.LastIndexOf(':') + 1);
        }

        public class IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Tenant.NameLength)]
            [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
            public string TenantName { get; set; }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (!Currency.IsNullOrWhiteSpace() && Currency != Constants.Models.Currency.Dkk)
            {
                results.Add(new ValidationResult($"The field {nameof(Currency)} only support the currency '{Constants.Models.Currency.Dkk}'.", [nameof(Currency)]));
            }

            return results;
        }
    }
}
