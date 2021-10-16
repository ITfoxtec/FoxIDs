using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity;

namespace FoxIDs.Models
{
    public abstract class ClaimTransform : IValidatableObject
    {
        [Required]
        [JsonProperty(PropertyName = "type")]
        public ClaimTransformTypes Type { get; set; }

        [Range(Constants.Models.Claim.TransformOrderMin, Constants.Models.Claim.TransformOrderMax)]
        [JsonProperty(PropertyName = "order")]
        public int Order { get; set; }

        [JsonProperty(PropertyName = "claims_in")]
        public abstract List<string> ClaimsIn { get; set; }

        [Required]
        [JsonProperty(PropertyName = "claim_out")]
        public abstract string ClaimOut { get; set; }

        [Required]
        [JsonProperty(PropertyName = "action")]
        public ClaimTransformActions Action { get; set; }

        [JsonProperty(PropertyName = "transformation")]
        public abstract string Transformation { get; set; }

        [JsonProperty(PropertyName = "transformation_extension")]
        public abstract string TransformationExtension { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            switch (Type)
            {
                case ClaimTransformTypes.Constant:
                    if (ClaimsIn?.Count() > 0)
                    {
                        results.Add(new ValidationResult($"The field {nameof(ClaimsIn)} can not be used with claim transformation type '{Type}'.", new[] { nameof(ClaimsIn) }));
                    }
                    if (Transformation.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", new[] { nameof(Transformation) }));
                    }
                    break;

                case ClaimTransformTypes.Match:
                case ClaimTransformTypes.RegexMatch:
                    if (ClaimsIn?.Count() != 1)
                    {
                        results.Add(new ValidationResult($"Exactly one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", new[] { nameof(ClaimsIn) }));
                    }
                    if (Transformation.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", new[] { nameof(Transformation) }));
                    }
                    if (TransformationExtension.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(TransformationExtension)} is required for claim transformation type '{Type}'.", new[] { nameof(TransformationExtension) }));
                    }
                    break;

                case ClaimTransformTypes.Map:
                    if (ClaimsIn?.Count() != 1)
                    {
                        results.Add(new ValidationResult($"Exactly one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", new[] { nameof(ClaimsIn) }));
                    }
                    break;

                case ClaimTransformTypes.RegexMap:
                    if (ClaimsIn?.Count() != 1)
                    {
                        results.Add(new ValidationResult($"Exactly one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", new[] { nameof(ClaimsIn) }));
                    }
                    if (Transformation.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", new[] { nameof(Transformation) }));
                    }
                    break;

                case ClaimTransformTypes.Concatenate:
                    if (ClaimsIn?.Count() < 1)
                    {
                        results.Add(new ValidationResult($"At least one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", new[] { nameof(ClaimsIn) }));
                    }
                    if (Transformation.IsNullOrWhiteSpace())
                    {
                        results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", new[] { nameof(Transformation) }));
                    }
                    break;

                default:
                    throw new NotSupportedException($"Claim transformation type '{Type}' not supported.");
            }
            return results;
        }
    }
}
