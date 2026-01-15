using System;
using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewDownPartySamlViewModel : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.IssuerLength)]
        [Display(Name = "Application issuer")]
        public string Issuer { get; set; }

        public bool IsManual { get; set; } = true;

        [Range(Constants.Models.SamlParty.MetadataUpdateRateMin, Constants.Models.SamlParty.MetadataUpdateRateMax)]
        [Display(Name = "Metadata update rate in seconds")]
        public int MetadataUpdateRate { get; set; } = 86400; // 24 hours

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        [Display(Name = "Metadata URL")]
        public string MetadataUrl { get; set; }

        [Display(Name = "Online metadata URL and automatic updates")]
        public bool AutomaticUpdate
        {
            get => !IsManual;
            set => IsManual = !value;
        }

        [ListLength(0, Constants.Models.SamlParty.Down.AcsUrlsMax, Constants.Models.SamlParty.Down.AcsUrlsLength)]
        [Display(Name = "Assertion consumer service (ACS) URL")]
        public List<string> AcsUrls { get; set; }

        [Display(Name = "Absolute URLs")]
        public bool DisableAbsoluteUrls { get; set; } = true;

        [Display(Name = "Authn request binding")]
        public SamlBindingTypes AuthnRequestBinding { get; set; } = SamlBindingTypes.Redirect;

        [Display(Name = "Authn response binding")]
        public SamlBindingTypes AuthnResponseBinding { get; set; } = SamlBindingTypes.Post;

        [Display(Name = "Encrypt authn response")]
        public bool EncryptAuthnResponse { get; set; }

        [Display(Name = "Logout request binding")]
        public SamlBindingTypes LogoutRequestBinding { get; set; } = SamlBindingTypes.Post;

        [Display(Name = "Logout response binding")]
        public SamlBindingTypes LogoutResponseBinding { get; set; } = SamlBindingTypes.Post;

        [MaxLength(Constants.Models.SamlParty.Down.SingleLogoutUrlLength)]
        [Display(Name = "Optional single logout URL")]
        public string SingleLogoutUrl { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.SamlParty.Down.KeysMin, Constants.Models.SamlParty.KeysMax)]
        [Display(Name = "Optional one or more signature validation certificates")]
        public List<JwkWithCertificateInfo> Keys { get; set; }

        [Display(Name = "Optional encryption certificate")]
        public JwkWithCertificateInfo EncryptionKey { get; set; }

        [Display(Name = "SAML 2.0 metadata")]
        public string Metadata { get; set; }

        [Display(Name = "IdP Issuer")]
        public string MetadataIssuer { get; set; }

        [Display(Name = "Single Sign-On URL")]
        public string MetadataAuthn { get; set; }

        [Display(Name = "Single Logout URL")]
        public string MetadataLogout { get; set; }

        public KeyInfoViewModel IdPKeyInfo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (IsManual)
            {
                if (AcsUrls == null || AcsUrls.Count < Constants.Models.SamlParty.Down.AcsUrlsMin)
                {
                    results.Add(new ValidationResult($"The field {nameof(AcsUrls)} must be at least {Constants.Models.SamlParty.Down.AcsUrlsMin}.", new[] { nameof(AcsUrls) }));
                }
            }
            else
            {
                if (MetadataUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The {nameof(MetadataUrl)} field is required.", new[] { nameof(MetadataUrl) }));
                }
            }

            return results;
        }
    }
}
