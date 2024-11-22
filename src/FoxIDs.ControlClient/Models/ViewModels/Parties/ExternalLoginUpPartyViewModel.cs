using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ExternalLoginUpPartyViewModel : IClaimTransformViewModel, IUpPartySessionLifetime, IUpPartyHrd, IClientAdditionalParameters
    {
        public string InitName { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        /// <summary>
        /// Default 10 hours.
        /// </summary>
        [Range(Constants.Models.UpParty.SessionLifetimeMin, Constants.Models.UpParty.SessionLifetimeMax)]
        public int SessionLifetime { get; set; } = 36000;

        /// <summary>
        /// Default 24 hours.
        /// </summary>
        [Range(Constants.Models.UpParty.SessionAbsoluteLifetimeMin, Constants.Models.UpParty.SessionAbsoluteLifetimeMax)]
        public int SessionAbsoluteLifetime { get; set; } = 86400;

        /// <summary>
        /// Default 0 minutes.
        /// </summary>
        [Range(Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMin, Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMax)]
        public int PersistentSessionAbsoluteLifetime { get; set; } = 0;

        /// <summary>
        /// Default false.
        /// </summary>
        public bool PersistentSessionLifetimeUnlimited { get; set; } = false;

        [Display(Name = "Single logout")]
        public bool DisableSingleLogout { get; set; }

        [Required]
        [Display(Name = "External login type")]
        public ExternalConnectTypes ExternalLoginType { get; set; } = ExternalConnectTypes.Api;

        [Required]
        [Display(Name = "Username type")]
        public ExternalLoginUsernameTypes UsernameType { get; set; } = ExternalLoginUsernameTypes.Email;

        [Required]
        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; }

        [Required]
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "API Secret")]
        public string Secret { get; set; }
        public string SecretLoaded { get; set; }

        /// <summary>
        /// Default false.
        /// </summary>
        [Required]
        [Display(Name = "Users can cancel login")]
        public bool EnableCancelLogin { get; set; } = false;

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Forward claims (use * to carried all claims forward)")]
        public List<string> Claims { get; set; } = new List<string>(["*"]);

        [ListLength(Constants.Models.OAuthUpParty.Client.AdditionalParametersMin, Constants.Models.OAuthUpParty.Client.AdditionalParametersMax)]
        [Display(Name = "Additional parameters")]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; } = new List<OAuthAdditionalParameter>();

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        /// <summary>
        /// Default if required.
        /// </summary>
        [Required]
        [Display(Name = "Logout consent")]
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; } = LoginUpPartyLogoutConsents.IfRequired;

        [MaxLength(Constants.Models.LoginUpParty.TitleLength)]
        [Display(Name = "Browser Title (otherwise Settings -> Display name or default FoxIDs)")]
        public string Title { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.IconUrlLength)]
        [Display(Name = "Browser Icon URL (https://example.somewhere/favicon.ico)")]
        public string IconUrl { get; set; }        

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [Display(Name = "CSS")]
        public string Css { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Show HRD button with domain")]
        public bool HrdShowButtonWithDomain { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) display name.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdDisplayNameLength)]
        [RegularExpression(Constants.Models.UpParty.HrdDisplayNameRegExPattern)]
        [Display(Name = "HRD display name")]
        public string HrdDisplayName { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) logo URL.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdLogoUrlLength)]
        [RegularExpression(Constants.Models.UpParty.HrdLogoUrlRegExPattern)]
        [Display(Name = "HRD logo URL")]
        public string HrdLogoUrl { get; set; }

        [ValidateComplexType]
        public LinkExternalUserViewModel LinkExternalUser { get; set; } = new LinkExternalUserViewModel();

        [ValidateComplexType]
        [ListLength(Constants.Models.UpParty.ProfilesMin, Constants.Models.UpParty.ProfilesMax)]
        public List<ExternalLoginUpPartyProfileViewModel> Profiles { get; set; } = new List<ExternalLoginUpPartyProfileViewModel>();
    }
}
