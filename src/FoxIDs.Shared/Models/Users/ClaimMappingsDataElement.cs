﻿using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class ClaimMappingsDataElement : IDataElement
    {
        public static async  Task<string> IdFormat(Track.IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"claimmap:{idKey.TenantName}:{idKey.TrackName}";
        }

        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapIdLength)]
        [RegularExpression(@"^[\w:_-]*$")]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [Length(Constants.Models.Claim.ClaimsMapMin, Constants.Models.Claim.ClaimsMapMax)]
        [JsonProperty(PropertyName = "mappings")]
        public IEnumerable<ClaimMap> Mappings { get; set; }

        public async Task SetIdAsync(Track.IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
        }
    }
}
