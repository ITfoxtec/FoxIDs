using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class UiLoginUpPartyData : DataDocument, IUiLoginUpParty
    {
        [Required]
        [MaxLength(Constants.Models.Party.IdLength)]
        [RegularExpression(Constants.Models.Party.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "disable_reset_password")]
        public bool DisableResetPassword { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [JsonProperty(PropertyName = "css")]
        public string Css { get; set; }
    }
}
