using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class SamlUpPartyProfile 
    {
        [Required]
        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Set authn context comparison or replace is already exist.
        /// </summary>
        public SamlAuthnContextComparisonTypes? AuthnContextComparison { get; set; }

        /// <summary>
        /// Set authn context class references or replace is already exist.
        /// </summary>
        [ListLength(Constants.Models.SamlParty.Up.AuthnContextClassReferencesMin, Constants.Models.SamlParty.Up.AuthnContextClassReferencesMax, Constants.Models.Claim.LimitedValueLength)]
        public List<string> AuthnContextClassReferences { get; set; }

        /// <summary>
        /// Set authn request extensions XML or replace is already exist.
        /// </summary>
        [MaxLength(Constants.Models.SamlParty.Up.AuthnRequestExtensionsXmlLength)]
        public string AuthnRequestExtensionsXml { get; set; }
    }
}
