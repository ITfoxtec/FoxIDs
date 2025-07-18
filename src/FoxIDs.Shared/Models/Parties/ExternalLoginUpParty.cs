﻿using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    /// <summary>
    /// External login.
    /// </summary>
    public class ExternalLoginUpParty : UpPartyWithExternalUser<ExternalLoginUpPartyProfile>, IOAuthClaimTransformsRef, IValidatableObject
    {
        public ExternalLoginUpParty()
        {
            Type = PartyTypes.ExternalLogin;
        }

        [Required]
        [JsonProperty(PropertyName = "external_login_type")]
        public ExternalConnectTypes ExternalLoginType { get; set; }

        [Required]
        [JsonProperty(PropertyName = "username_type")]
        public ExternalLoginUsernameTypes UsernameType { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [JsonProperty(PropertyName = "api_url")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.AdditionalParametersMin, Constants.Models.OAuthUpParty.Client.AdditionalParametersMax)]
        [JsonProperty(PropertyName = "additional_parameter")]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; }

        [Required]
        [JsonProperty(PropertyName = "enable_cancel_login")]
        public bool EnableCancelLogin { get; set; }

        [Required]
        [JsonProperty(PropertyName = "logout_consent")]
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claims")]
        public List<string> Claims { get; set; }

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.TitleLength)]
        [RegularExpression(Constants.Models.LoginUpParty.TitleRegExPattern)]
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.IconUrlLength)]
        [JsonProperty(PropertyName = "icon_url")]
        public string IconUrl { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [JsonProperty(PropertyName = "css")]
        public string Css { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }

            if (ExternalLoginType == ExternalConnectTypes.Api)
            {
                if (ApiUrl.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(ApiUrl)}' is required if the {nameof(ExternalLoginType)} is '{ExternalLoginType}'.", [nameof(ApiUrl), nameof(ExternalLoginType)]));
                }

                if (Secret.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(Secret)}' is required if the {nameof(ExternalLoginType)} is '{ExternalLoginType}'.", [nameof(Secret), nameof(ExternalLoginType)]));
                }
            }

            if (UsernameType == ExternalLoginUsernameTypes.Text)
            {
                if(HrdDomains?.Count() > 0)
                {
                    results.Add(new ValidationResult($"HRD domains in the field '{nameof(HrdDomains)}' is not allowed if the {nameof(UsernameType)} is '{UsernameType}'.", [nameof(ApiUrl), nameof(UsernameType)]));
                }
            }

            return results;
        }
    }
}
