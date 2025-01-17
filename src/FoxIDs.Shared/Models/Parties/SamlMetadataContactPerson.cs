using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    /// <summary>
    /// The ContactPerson element specifies basic contact information about a person responsible in some
    /// capacity for a SAML entity or role. The use of this element is always optional. Its content is informative in
    /// nature and does not directly map to any core SAML elements or attributes.
    /// </summary>
    public class SamlMetadataContactPerson
    {
        /// <summary>
        /// Specifies the type of contact using the ContactTypeType enumeration. The possible values are
        /// technical, support, administrative, billing, and other.
        /// </summary>
        [Required]
        [JsonProperty(PropertyName = "contact_type")]
        public SamlMetadataContactPersonTypes ContactType { get; set; }

        /// <summary>
        /// Optional string element that specifies the name of the company for the contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "company")]
        public string Company { get; set; }

        /// <summary>
        /// Optional string element that specifies the given (first) name of the contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "given_name")]
        public string GivenName { get; set; }

        /// <summary>
        /// Optional string element that specifies the surname of the contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "surname")]
        public string Surname { get; set; }

        /// <summary>
        /// Optional string element containing mailto: URIs representing email addresses belonging to the
        /// contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "email_address")]
        public string EmailAddress { get; set; }

        /// <summary>
        /// Optional string element specifying a telephone number of the contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "telephone_number")]
        public string TelephoneNumber { get; set; }
    }
}
