using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class AuthCodeTtlGrant : DataTtlDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.AuthCodeTtlGrant}:{idKey.TenantName}:{idKey.TrackName}:{idKey.Code}";
        }

        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.Grant.IdLength)]
        [RegularExpression(@"^[\w:\-_]*$")]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [ListLength(1, 1000)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [JsonProperty(PropertyName = "client_id")]
        public string ClientId { get; set; }

        [MaxLength(Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "redirect_uri")]
        public string RedirectUri { get; set; }

        [MaxLength(IdentityConstants.MessageLength.ScopeMax)]
        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }

        [MaxLength(IdentityConstants.MessageLength.NonceMax)]
        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }

        [MaxLength(IdentityConstants.MessageLength.CodeChallengeMax)]
        [JsonProperty(PropertyName = "code_challenge")]
        public string CodeChallenge { get; set; }

        [MaxLength(IdentityConstants.MessageLength.CodeChallengeMethodMax)]
        [JsonProperty(PropertyName = "code_challenge_method")]
        public string CodeChallengeMethod { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
        }

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(100)]
            [RegularExpression(@"^[\w-_]*$")]
            public string Code { get; set; }
        }
    }
}
