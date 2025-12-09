using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to ingest SAML metadata from a URL or XML payload.
    /// </summary>
    public class SamlReadMetadataRequest : IValidatableObject
    {
        /// <summary>
        /// Indicates whether metadata is provided as a URL or raw XML.
        /// </summary>
        [Required]
        public SamlReadMetadataType Type { get; set; }

        /// <summary>
        /// Metadata source (URL or XML string).
        /// </summary>
        [Required]
        public string Metadata { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            switch (Type)
            {
                case SamlReadMetadataType.Url:
                    if (Metadata.Length > Constants.Models.SamlParty.MetadataUrlLength)
                    {
                        results.Add(new ValidationResult($"The field {nameof(Metadata)} must be a string with a maximum length of '{Constants.Models.SamlParty.MetadataUrlLength}'.", new[] { nameof(Metadata) }));
                    }
                    break;
                case SamlReadMetadataType.Xml:
                    if (Metadata.Length > Constants.Models.SamlParty.MetadataXmlSize)
                    {
                        results.Add(new ValidationResult($"The field {nameof(Metadata)} must be a string with a maximum length of '{Constants.Models.SamlParty.MetadataXmlSize}'.", new[] { nameof(Metadata) }));
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            return results;
        }
    }
}
