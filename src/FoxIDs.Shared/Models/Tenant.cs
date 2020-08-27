﻿using Newtonsoft.Json;
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

        public static async Task<string> IdFormat(string name)
        {
            if (name == null) new ArgumentNullException(nameof(name));

            return await IdFormat(new IdKey
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
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
            Name = Id.Substring(Id.LastIndexOf(':') + 1);
        }

        public class IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Tenant.NameLength)]
            [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
            public string TenantName { get; set; }
        }
    }
}
