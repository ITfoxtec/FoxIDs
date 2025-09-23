using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
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

        [JsonProperty(PropertyName = "disable_reset_password")]
        public bool? DisableResetPassword
        {
            get { return null; }
            set
            {
                if (value == true)
                {
                    DisableSetPassword = true;
                }
            }
        }

        [JsonProperty(PropertyName = "disable_set_new_password")]
        public bool DisableSetPassword { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.TitleLength)]
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.IconUrlLength)]
        [JsonProperty(PropertyName = "icon_url")]
        public string IconUrl { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [JsonProperty(PropertyName = "css")]
        public string Css { get; set; }

        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [JsonProperty(PropertyName = "login_elements")]
        public List<DynamicElement> Elements { get; set; }
    }
}
