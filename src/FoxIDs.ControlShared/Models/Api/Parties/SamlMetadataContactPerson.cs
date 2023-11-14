using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// The ContactPerson element specifies basic contact information about a person responsible in some
    /// capacity for a SAML entity or role. The use of this element is always optional. Its content is informative in
    /// nature and does not directly map to any core SAML elements or attributes.
    /// </summary>
    public class SamlMetadataContactPerson
    {
        /// <summary>
        /// [Required]
        /// Specifies the type of contact using the ContactTypeType enumeration. The possible values are
        /// technical, support, administrative, billing, and other.
        /// </summary>
        [Required]
        public SamlMetadataContactPersonTypes ContactType { get; set; }

        /// <summary>
        /// [Optional]
        /// Optional string element that specifies the name of the company for the contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Company")]
        public string Company { get; set; }

        /// <summary>
        /// [Optional]
        /// Optional string element that specifies the given (first) name of the contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Given name")]
        public string GivenName { get; set; }

        /// <summary>
        /// [Optional]
        /// Optional string element that specifies the surname of the contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Surname")]
        public string Surname { get; set; }

        /// <summary>
        /// [Optional]
        /// Optional string element containing mailto: URIs representing e-mail addresses belonging to the
        /// contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        /// <summary>
        /// [Optional]
        /// Optional string element specifying a telephone number of the contact person.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Telephone number")]
        public string TelephoneNumber { get; set; }
    }
}
