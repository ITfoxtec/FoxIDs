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

        [JsonProperty(PropertyName = "claim_out")]
        public abstract string ClaimOut { get; set; }

        [Required]
        [JsonProperty(PropertyName = "action")]
        public ClaimTransformActions Action { get; set; } = ClaimTransformActions.Add;

        [JsonProperty(PropertyName = "transformation")]
        public abstract string Transformation { get; set; }

        [JsonProperty(PropertyName = "transformation_extension")]
        public abstract string TransformationExtension { get; set; }

        [JsonProperty(PropertyName = "external_connect_type")]
        public ExternalConnectTypes ExternalConnectType { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [JsonProperty(PropertyName = "api_url")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if(Type != ClaimTransformTypes.ExternalClaims && ClaimOut.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ClaimOut)} is required for claim transformation type '{Type}'.", [nameof(ClaimOut)]));
            }

            if (Action == ClaimTransformActions.Add || Action == ClaimTransformActions.Replace)
            {
                switch (Type)
                {
                    case ClaimTransformTypes.Constant:
                        if (Transformation.IsNullOrWhiteSpace())
                        {
                            results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", [nameof(Transformation)]));
                        }
                        break;

                    case ClaimTransformTypes.MatchClaim:
                        ValidateMatchClaimAddReplace(results);
                        break;

                    case ClaimTransformTypes.Match:
                    case ClaimTransformTypes.RegexMatch:
                        ValidateMatchAddReplace(results);
                        break;

                    case ClaimTransformTypes.Map:
                    case ClaimTransformTypes.DkPrivilege:
                        ValidateMapClaimAddReplace(results);
                        break;

                    case ClaimTransformTypes.RegexMap:
                        ValidateRegexMapClaimAddReplace(results);
                        break;

                    case ClaimTransformTypes.Concatenate:
                        if (ClaimsIn?.Count() < 1)
                        {
                            results.Add(new ValidationResult($"At least one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", [nameof(ClaimsIn)]));
                        }
                        if (Transformation.IsNullOrWhiteSpace())
                        {
                            results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", [nameof(Transformation)]));
                        }
                        break;

                    case ClaimTransformTypes.ExternalClaims:
                        if (ClaimsIn?.Count() < 1)
                        {
                            results.Add(new ValidationResult($"At least one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", [nameof(ClaimsIn)]));
                        }
                        if (ExternalConnectType == ExternalConnectTypes.Api)
                        {
                            if (ApiUrl.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field {nameof(ApiUrl)} is required for claim transformation type '{Type}' and external connect type '{ExternalConnectType}'.", [nameof(ApiUrl)]));
                            }
                            if (Secret.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field {nameof(Secret)} is required for claim transformation type '{Type}' and external connect type '{ExternalConnectType}'.", [nameof(Secret)]));
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"Claim transformation type '{Type}' and external connect type '{ExternalConnectType}' not supported.");
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Claim transformation type '{Type}' not supported.");
                }
            }
            else if (Action == ClaimTransformActions.AddIfNot || Action == ClaimTransformActions.ReplaceIfNot)
            {
                switch (Type)
                {
                    case ClaimTransformTypes.MatchClaim:
                        ValidateMatchClaimAddReplace(results);
                        break;

                    case ClaimTransformTypes.Match:
                    case ClaimTransformTypes.RegexMatch:
                        ValidateMatchAddReplace(results);
                        break;

                    default:
                        throw new NotSupportedException($"Claim transformation type '{Type}' not supported.");
                }
            }
            else if (Action == ClaimTransformActions.AddIfNotOut)
            {
                switch (Type)
                {
                    case ClaimTransformTypes.Map:
                        ValidateMapClaimAddReplace(results);
                        break;

                    case ClaimTransformTypes.RegexMap:
                        ValidateRegexMapClaimAddReplace(results);
                        break;

                    default:
                        throw new NotSupportedException($"Claim transformation type '{Type}' not supported.");
                }
            }
            else if (Action == ClaimTransformActions.Remove)
            {
                switch (Type)
                {
                    case ClaimTransformTypes.MatchClaim:
                        break;

                    case ClaimTransformTypes.Match:
                    case ClaimTransformTypes.RegexMatch:
                        if (Transformation.IsNullOrWhiteSpace())
                        {
                            results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", [nameof(Transformation)]));
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Claim transformation type '{Type}' not supported.");
                }
            }
            return results;
        }

        private void ValidateMatchClaimAddReplace(List<ValidationResult> results)
        {
            if (ClaimsIn?.Count() != 1)
            {
                results.Add(new ValidationResult($"Exactly one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", [nameof(ClaimsIn)]));
            }
            if (Transformation.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", [nameof(Transformation)]));
            }
        }

        private void ValidateMatchAddReplace(List<ValidationResult> results)
        {
            if (ClaimsIn?.Count() != 1)
            {
                results.Add(new ValidationResult($"Exactly one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", [nameof(ClaimsIn)]));
            }
            if (Transformation.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", [nameof(Transformation)]));
            }
            if (TransformationExtension.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(TransformationExtension)} is required for claim transformation type '{Type}'.", [nameof(TransformationExtension)]));
            }
        }

        private void ValidateMapClaimAddReplace(List<ValidationResult> results)
        {
            if (ClaimsIn?.Count() != 1)
            {
                results.Add(new ValidationResult($"Exactly one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", [nameof(ClaimsIn)]));
            }
        }

        private void ValidateRegexMapClaimAddReplace(List<ValidationResult> results)
        {
            if (ClaimsIn?.Count() != 1)
            {
                results.Add(new ValidationResult($"Exactly one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", [nameof(ClaimsIn)]));
            }
            if (Transformation.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation type '{Type}'.", [nameof(Transformation)]));
            }
        }
    }
}
