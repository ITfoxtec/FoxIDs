using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class ActiveSessionTtl : DataTtlDocument
    {
        public static readonly int DefaultTimeToLive = Constants.Models.OAuthDownParty.Client.AccessTokenLifetimeMax + Constants.Models.OAuthDownParty.Client.AuthorizationCodeLifetimeMax + Constants.Models.Session.AdditionalLifetimeMax;

        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) throw new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.ActiveSession}:{idKey.TenantName}:{idKey.TrackName}:{idKey.SessionIdHash}";
        }

        [Required]
        [MaxLength(Constants.Models.Session.IdLength)]
        [RegularExpression(Constants.Models.Session.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [JsonProperty(PropertyName = "session_id")]
        public string SessionId { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "sub")]
        public string Sub { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "sub_format")]
        public string SubFormat { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "phone")]
        public string Phone { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        [ListLength(Constants.Models.Session.LinksMin, Constants.Models.Session.LinksMax)]
        [JsonProperty(PropertyName = "up_party_links")]
        public List<PartyNameSessionLink> UpPartyLinks { get; set; }

        [JsonProperty(PropertyName = "session_up_party")]
        public PartyNameSessionLink SessionUpParty { get; set; }

        [ListLength(Constants.Models.Session.LinksMin, Constants.Models.Session.LinksMax)]
        [JsonProperty(PropertyName = "down_party_links")]
        public List<PartyNameSessionLink> DownPartyLinks { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "client_ip")]
        public string ClientIp { get; set; }

        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "user_agent")]
        public string UserAgent { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; }

        [JsonProperty(PropertyName = "lu")]
        public long LastUpdated { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) throw new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Session.SessionIdHashLength)]
            public string SessionIdHash { get; set; }
        }
    }
}
