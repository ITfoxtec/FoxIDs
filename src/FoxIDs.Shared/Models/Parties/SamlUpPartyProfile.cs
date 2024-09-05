using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class SamlUpPartyProfile : UpPartyProfile
    {
        /// <summary>
        /// Set authn context comparison or replace is already exist.
        /// </summary>
        [JsonProperty(PropertyName = "authn_context_comparison")]
        public SamlAuthnContextComparisonTypes? AuthnContextComparison { get; set; }

        /// <summary>
        /// Set authn context class references or replace is already exist.
        /// </summary>
        [ListLength(Constants.Models.SamlParty.Up.AuthnContextClassReferencesMin, Constants.Models.SamlParty.Up.AuthnContextClassReferencesMax, Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "authn_context_class_refs")]
        public List<string> AuthnContextClassReferences { get; set; }

        /// <summary>
        /// Set authn request extensions XML or replace is already exist.
        /// </summary>
        [MaxLength(Constants.Models.SamlParty.Up.AuthnRequestExtensionsXmlLength)]
        [JsonProperty(PropertyName = "authn_request_extensions_xml")]
        public string AuthnRequestExtensionsXml { get; set; }
    }
}
