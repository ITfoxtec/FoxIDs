using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
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

        [Obsolete("Delete after 2026-06-01.")]
        [JsonProperty(PropertyName = "disable_reset_password")]
        public bool? DisableResetPassword
        {
            get { return null; }
            set
            {
                if (value == true)
                {
                    DisableSetPasswordSms = true;
                    DisableSetPasswordEmail = true;
                }
            }
        }

        [Obsolete("Delete after 2026-10-02.")]
        [JsonProperty(PropertyName = "disable_set_new_password")]
        public bool? DisableSetPassword
        {
            get { return null; }
            set
            {
                if (value == true)
                {
                    DisableSetPasswordSms = true;
                    DisableSetPasswordEmail = true;
                }
            }
        }

        [JsonProperty(PropertyName = "disable_set_password_sms")]
        public bool DisableSetPasswordSms { get; set; }

        [JsonProperty(PropertyName = "disable_set_password_email")]
        public bool DisableSetPasswordEmail { get; set; }

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