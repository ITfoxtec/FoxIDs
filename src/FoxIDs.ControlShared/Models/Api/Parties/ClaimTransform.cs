using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity;

namespace FoxIDs.Models.Api
{
    public abstract class ClaimTransform : IValidatableObject
    {
        [MaxLength(Constants.Models.Claim.TransformNameLength)]
        [RegularExpression(Constants.Models.Claim.TransformNameRegExPattern)]
        public string Name { get; set; }

        [Required]
        public ClaimTransformTypes Type { get; set; }

        [Range(Constants.Models.Claim.TransformOrderMin, Constants.Models.Claim.TransformOrderMax)]
        public int Order { get; set; }

        [Display(Name = "Select claims")]
        public abstract List<string> ClaimsIn { get; set; }

        public abstract string ClaimOut { get; set; }

        [Required]
        [Display(Name = "Action")]
        public ClaimTransformActions Action { get; set; } = ClaimTransformActions.Add;

        [Display(Name = "Transformation")]
        public abstract string Transformation { get; set; }

        [Display(Name = "Transformation extension")]
        public abstract string TransformationExtension { get; set; }

        #region ExternalClaims
        [Display(Name = "External connect type")]
        public ExternalConnectTypes ExternalConnectType { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "API secret")]
        public string Secret { get; set; }

        /// <summary>
        /// API secret loaded. Used to compare loaded and updated value.
        /// </summary>
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        public string SecretLoaded { get; set; }
        #endregion

        #region Task
        [Display(Name = "Task")]
        public ClaimTransformTasks Task { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Error")]
        public string Error { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Error message")]
        public string ErrorMessage { get; set; }

        [Display(Name = "Authentication type")]
        public PartyTypes UpPartyType { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Authentication name")]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Authentication profile name")]
        public string UpPartyProfileName { get; set; }
        #endregion

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Task == ClaimTransformTasks.None && Type != ClaimTransformTypes.ExternalClaims && ClaimOut.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ClaimOut)} is required for claim transformation type '{Type}'.", [nameof(ClaimOut)]));
            }

            if (Task != ClaimTransformTasks.None)
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
                                results.Add(new ValidationResult($"The field and {nameof(ErrorMessage)} is required for claim transformation task '{Task}'.", [nameof(ErrorMessage)]));
                            }
                            break;
                        case ClaimTransformTasks.UpPartyAction:
                            if (UpPartyType == PartyTypes.None || UpPartyName.IsNullOrWhiteSpace())
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
                            if (Transformation.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field {nameof(Transformation)} is required for claim transformation task '{Task}'.", [nameof(Transformation)]));
                            }
                            break;
                        case ClaimTransformTasks.QueryExternalUser:
                            if (UpPartyName.IsNullOrWhiteSpace() || Transformation.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The fields {nameof(UpPartyName)} and {nameof(Transformation)} is required for claim transformation task '{Task}'.", [nameof(UpPartyName), nameof(Transformation)]));
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
