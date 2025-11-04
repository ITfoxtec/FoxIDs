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
        [MaxLength(Constants.Models.Claim.TransformNameLength)]
        [RegularExpression(Constants.Models.Claim.TransformNameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

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

        #region ExternalClaims
        [JsonProperty(PropertyName = "external_connect_type")]
        public ExternalConnectTypes? ExternalConnectType { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [JsonProperty(PropertyName = "api_url")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; set; }
        #endregion

        #region Task
        [JsonProperty(PropertyName = "task")]
        public ClaimTransformTasks? Task { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [JsonProperty(PropertyName = "error_message")]
        public string ErrorMessage { get; set; }

        [JsonProperty(PropertyName = "up_party_type")]
        public PartyTypes? UpPartyType { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "up_party_name")]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "up_party_profile_name")]
        public string UpPartyProfileName { get; set; }
        #endregion

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if(Task == null && Type != ClaimTransformTypes.ExternalClaims && ClaimOut.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ClaimOut)} is required for claim transformation type '{Type}'.", [nameof(ClaimOut)]));
            }

            if (Task != null)
            {
                if (Action == ClaimTransformActions.If || Action == ClaimTransformActions.IfNot)
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
                            throw new NotSupportedException($"Claim transformation type '{Type}' is not supported with action '{Action}'.");
                    }

                    switch (Task)
                    {
                        case ClaimTransformTasks.RequestException:
                            if (ErrorMessage.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field {nameof(ErrorMessage)} is required for claim transformation task '{Task}'.", [nameof(ErrorMessage)]));
                            }
                            break;
                        case ClaimTransformTasks.UpPartyAction:
                            if (UpPartyType == null || UpPartyName.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The fields {nameof(UpPartyType)} and {nameof(UpPartyName)} is required for claim transformation task '{Task}'.", [nameof(UpPartyType), nameof(UpPartyName)]));
                            }
                            break;
                        default:
                            throw new NotSupportedException($"Claim transformation task '{Task}' is not supported with type '{Type}' and action '{Action}'.");
                    }
                }
                else if (Action == ClaimTransformActions.Add || Action == ClaimTransformActions.Replace)
                {
                    switch (Type)
                    {
                        case ClaimTransformTypes.MatchClaim:
                            ValidateMatchClaimAddReplace(results);
                            break;

                        default:
                            throw new NotSupportedException($"Claim transformation type '{Type}' is not supported with action '{Action}'.");
                    }

                    switch (Task)
                    {
                        case ClaimTransformTasks.QueryInternalUser:
                            ValidateMatchClaimAddReplace(results);
                            if (Transformation.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation task '{Task}'.", [nameof(Transformation)]));
                            }
                            break;
                        case ClaimTransformTasks.QueryExternalUser:
                            ValidateMatchClaimAddReplace(results);
                            if (UpPartyName.IsNullOrWhiteSpace() || Transformation.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The fields {nameof(UpPartyName)} and {nameof(Transformation)} is required for claim transformation task '{Task}'.", [nameof(UpPartyName), nameof(Transformation)]));
                            }
                            break;
                        case ClaimTransformTasks.SaveClaimInternalUser:
                            ValidateMatchClaimAddReplace(results);
                            if (Transformation.IsNullOrWhiteSpace() || TransformationExtension.IsNullOrWhiteSpace() || ClaimOut.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The fields {nameof(Transformation)}, {nameof(TransformationExtension)} and {nameof(ClaimOut)} is required for claim transformation task '{Task}'.", [nameof(Transformation), nameof(TransformationExtension), nameof(ClaimOut)]));
                            }
                            break;
                        case ClaimTransformTasks.SaveClaimExternalUser:
                            ValidateMatchClaimAddReplace(results);
                            if (UpPartyName.IsNullOrWhiteSpace() || Transformation.IsNullOrWhiteSpace() || TransformationExtension.IsNullOrWhiteSpace() || ClaimOut.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The fields {nameof(UpPartyName)}, {nameof(Transformation)}, {nameof(TransformationExtension)} and {nameof(ClaimOut)} is required for claim transformation task '{Task}'.", [nameof(UpPartyName), nameof(Transformation), nameof(TransformationExtension), nameof(ClaimOut)]));
                            }
                            break;
                        default:
                            throw new NotSupportedException($"Claim transformation task '{Task}' is not supported with type '{Type}' and action '{Action}'.");
                    }
                }
            }
            else
            {
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
                            if (Name.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field {nameof(Name)} is required for claim transformation type '{Type}'.", [nameof(Name)]));
                            }
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
            }
            return results;
        }

        private void ValidateMatchClaimAddReplace(List<ValidationResult> results)
        {
            if (ClaimsIn?.Count() != 1)
            {
                results.Add(new ValidationResult($"Exactly one is required in the field {nameof(ClaimsIn)} for claim transformation type '{Type}'.", [nameof(ClaimsIn)]));
            }
            if (Task == null && Transformation.IsNullOrWhiteSpace())
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
            if (Task == null && TransformationExtension.IsNullOrWhiteSpace())
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
