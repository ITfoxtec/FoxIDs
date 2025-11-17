using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class AccessTokenSessionTtl : DataTtlDocument
    {
        public static readonly int DefaultTimeToLive = Constants.Models.OAuthDownParty.Client.AccessTokenLifetimeMax + Constants.Models.OAuthDownParty.AccessTokenSession.AdditionalLifetimeMax;

        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) throw new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.AccessTokenSession}:{idKey.TenantName}:{idKey.TrackName}:{idKey.SessionIdHash}";
        }

        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.AccessTokenSession.IdLength)]
        [RegularExpression(Constants.Models.OAuthDownParty.AccessTokenSession.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(IdentityConstants.MessageLength.SessionIdMax)]
        [JsonProperty(PropertyName = "session_id")]
        public string SessionId { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) throw new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
            SessionId = idKey.SessionIdHash;
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.OAuthDownParty.AccessTokenSession.SessionIdHashLength)]
            public string SessionIdHash { get; set; }
        }
    }
}
