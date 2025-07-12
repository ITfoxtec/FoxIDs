using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity;
using System.Linq;
using System;

namespace FoxIDs.Models.Api
{
    public class OidcUpParty : INameValue, INewNameValue, IClaimTransformRef<OAuthClaimTransform>, IExtendedUisRef, ILinkExternalUserRef, IExitClaimTransformsRef<OAuthClaimTransform>, IValidatableObject
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string NewName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        [Required]
        public PartyUpdateStates UpdateState { get; set; } = PartyUpdateStates.Automatic;

        [Range(Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMin, Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMax)]
        public int? OidcDiscoveryUpdateRate { get; set; } = 172800; // 2 days

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.AuthorityLength)]
        public string Authority { get; set; }

        public bool? EditIssuersInAutomatic { get; set; }

        /// <summary>
        /// Use * to accept all issuers, only possible if the issuer is edited.
        /// </summary>
        [ListLength(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        public List<string> Issuers { get; set; }

        /// <summary>
        /// SP issuer / audience
        /// Only used in relation to token exchange trust.
        /// </summary>
        [MaxLength(Constants.Models.Party.IssuerLength)]
        public string SpIssuer { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.KeysApiMin, Constants.Models.OAuthUpParty.KeysMax)]
        public List<JwkWithCertificateInfo> Keys { get; set; }

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

        public bool DisableSingleLogout { get; set; }

        /// <summary>
        /// OIDC up client.
        /// </summary>
        [Required]
        public OidcUpClient Client { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        /// <summary>
        /// Extended UIs.
        /// </summary>
        [ListLength(Constants.Models.ExtendedUi.UisMin, Constants.Models.ExtendedUi.UisMax)]
        public List<ExtendedUi> ExtendedUis { get; set; }

        /// <summary>
        /// Claim transforms executed before exit / response from up-party and after the external users claims has been loaded.
        /// </summary>
        [Obsolete("Delete after 2026-07-01.")]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ExternalUserLoadedClaimTransforms
        {
            get
            {
                return ExitClaimTransforms;
            }
            set
            {
                if (value?.Count > 0)
                {
                    ExitClaimTransforms = ExternalUserLoadedClaimTransforms;
                }
            }
        }

        /// <summary>
        /// Claim transforms executed before exit / response from up-party and after the external users claims has been loaded.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ExitClaimTransforms { get; set; }

        /// <summary>
        /// URL binding pattern.
        /// </summary>
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        /// <summary>
        /// Home realm discovery (HRD) IP addresses and IP ranges.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdIPAddressAndRangeMin, Constants.Models.UpParty.HrdIPAddressAndRangeMax, Constants.Models.UpParty.HrdIPAddressAndRangeLength, Constants.Models.UpParty.HrdIPAddressAndRangeRegExPattern, Constants.Models.UpParty.HrdIPAddressAndRangeTotalMax)]
        [Display(Name = "HRD IP addresses and IP ranges")]
        public List<string> HrdIPAddressesAndRanges { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern, Constants.Models.UpParty.HrdDomainTotalMax)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) regular expressions.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdRegularExpressionMin, Constants.Models.UpParty.HrdRegularExpressionMax, Constants.Models.UpParty.HrdRegularExpressionLength, Constants.Models.UpParty.HrdRegularExpressionTotalMax)]
        [Display(Name = "HRD regular expressions")]
        public List<string> HrdRegularExpressions { get; set; }

        [Display(Name = "Show HRD button while using IP address / range, HRD domain or regular expression")]
        public bool HrdAlwaysShowButton { get; set; }

        [Display(Name = "Show HRD button while using HRD domain")]
        [Obsolete($"Use {nameof(HrdAlwaysShowButton)} instead.")]
        public bool? HrdShowButtonWithDomain { get; set; }

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

        [Display(Name = "Disable user authentication trust")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [Display(Name = "Disable token exchange trust")]
        public bool DisableTokenExchangeTrust { get; set; }

        [ValidateComplexType]
        public LinkExternalUser LinkExternalUser { get; set; }

        [ListLength(Constants.Models.UpParty.ProfilesMin, Constants.Models.UpParty.ProfilesMax)]
        [Display(Name = "Profiles")]
        public List<OidcUpPartyProfile> Profiles { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
            }
            if (DisableUserAuthenticationTrust && DisableTokenExchangeTrust)
            {
                results.Add(new ValidationResult($"Both the {nameof(DisableUserAuthenticationTrust)} and the {nameof(DisableTokenExchangeTrust)} can not be disabled at the same time.", [nameof(DisableUserAuthenticationTrust), nameof(DisableTokenExchangeTrust)]));
            }

            var clientResults = Client.ValidateFromParty(UpdateState, DisableUserAuthenticationTrust);
            if (clientResults.Count() > 0)
            {
                results.AddRange(clientResults);
            }

            if (UpdateState == PartyUpdateStates.Manual)
            {
                if (Issuers?.Count(i => !string.IsNullOrWhiteSpace(i)) <= 0)
                {
                    results.Add(new ValidationResult($"Require at least one issuer in '{nameof(Issuers)}'. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.", [nameof(Issuers)]));
                }

                if (Keys?.Count <= 0)
                {
                    results.Add(new ValidationResult($"Require at least one key in '{nameof(Keys)}'. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.", [nameof(Keys)]));
                }

                if (Client.AuthorizeUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Require '{nameof(Client)}.{nameof(Client.AuthorizeUrl)}'. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.", [$"{nameof(Client)}.{nameof(Client.AuthorizeUrl)}"]));
                }
            }
            else
            {
                if (!OidcDiscoveryUpdateRate.HasValue)
                {
                    results.Add(new ValidationResult($"Require '{nameof(OidcDiscoveryUpdateRate)}'. If '{nameof(UpdateState)}' is different from '{PartyUpdateStates.Manual}'.", [nameof(OidcDiscoveryUpdateRate)]));
                }
            }

            if (Profiles != null)
            {
                var count = 0;
                foreach (var profile in Profiles)
                {
                    count++;
                    if ((Name.Length + profile.Name?.Length) > Constants.Models.Party.NameLength)
                    {
                        results.Add(new ValidationResult($"The fields {nameof(Name)} (value: '{Name}') and {nameof(profile.Name)} (value: '{profile.Name}') must not be more then {Constants.Models.Party.NameLength} in total.", [nameof(Name), $"{nameof(profile)}[{count}].{nameof(profile.Name)}"]));
                    }
                }
            }

            if (ClaimTransforms?.Count() + ExitClaimTransforms?.Count() > Constants.Models.Claim.TransformsMax)
            {
                results.Add(new ValidationResult($"The number of claims transforms in '{nameof(ClaimTransforms)}' and '{nameof(ExitClaimTransforms)}' can be a  of {Constants.Models.Claim.TransformsMax} combined.", [nameof(ClaimTransforms), nameof(ExitClaimTransforms)]));
            }
            return results;
        }
    }
}
