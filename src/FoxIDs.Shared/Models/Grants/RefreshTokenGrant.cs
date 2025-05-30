﻿using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class RefreshTokenGrant : DataDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.RefreshTokenGrant}:{idKey.TenantName}:{idKey.TrackName}:{idKey.RefreshToken}";
        }

        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.Grant.IdLength)]
        [RegularExpression(Constants.Models.OAuthDownParty.Grant.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [JsonIgnore]
        public string RefreshToken { get => Id.Substring(Id.LastIndexOf(':') + 1); }

        [ListLength(Constants.Models.OAuthDownParty.Grant.ClaimsMin, Constants.Models.OAuthDownParty.Grant.ClaimsMax)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [JsonProperty(PropertyName = "client_id")]
        public string ClientId { get; set; }

        [MaxLength(IdentityConstants.MessageLength.ScopeMax)]
        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }

        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [JsonProperty(PropertyName = "session_id")]
        public string SessionId { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "sub")]
        public string Sub { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "phone")]
        public string Phone { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "up_party_name")]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "up_party_type")]
        public string UpPartyType { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.OAuthDownParty.Grant.RefreshTokenLength)]
            [RegularExpression(Constants.Models.OAuthDownParty.Grant.RefreshTokenRegExPattern)]
            public string RefreshToken { get; set; }
        }
    }
}
