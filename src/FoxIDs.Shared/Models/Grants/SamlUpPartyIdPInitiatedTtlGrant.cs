using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class SamlUpPartyIdPInitiatedTtlGrant : DataTtlDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.SamlUpPartyIdPInitiatedTtlGrant}:{idKey.TenantName}:{idKey.TrackName}:{idKey.Code}";
        }

        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.Grant.IdLength)]
        [RegularExpression(Constants.Models.OAuthDownParty.Grant.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [ListLength(Constants.Models.OAuthDownParty.Grant.ClaimsMin, Constants.Models.OAuthDownParty.Grant.ClaimsMax)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.IdLength)]
        [JsonProperty(PropertyName = "down_party_id")]
        public string DownPartyId { get; set; }

        [Required]
        [JsonProperty(PropertyName = "down_party_type")]
        public PartyTypes DownPartyType { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.OAuthDownParty.Grant.CodeLength)]
            [RegularExpression(Constants.Models.OAuthDownParty.Grant.CodeRegExPattern)]
            public string Code { get; set; }
        }
    }
}
