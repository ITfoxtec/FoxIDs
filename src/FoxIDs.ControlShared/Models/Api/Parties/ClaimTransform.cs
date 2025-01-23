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
        public ExternalConnectTypes? ExternalConnectType { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "API secret")]
        public string Secret { get; set; }
        #endregion

        #region TaskAction
        [Display(Name = "Task action")]
        public ClaimTransformTaskActions? TaskAction { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Error")]
        public string Error { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Error message")]
        public string ErrorMessage { get; set; }

        [Display(Name = "Authentication type")]
        public PartyTypes? UpPartyType { get; set; }

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

            if (Type != ClaimTransformTypes.ExternalClaims && ClaimOut.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ClaimOut)} is required for claim transformation type '{Type}'.", [nameof(ClaimOut)]));
            }

            if (TaskAction != null)
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

                    switch (TaskAction)
                    {
                        case ClaimTransformTaskActions.RequestException:
                            if (ErrorMessage.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field and {nameof(ErrorMessage)} is required for claim transformation task action '{TaskAction}'.", [nameof(ErrorMessage)]));
                            }
                            break;
                        case ClaimTransformTaskActions.UpPartyAction:
                            if (UpPartyType == null || UpPartyName.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The fields {nameof(UpPartyType)} and {nameof(UpPartyName)} is required for claim transformation task action '{TaskAction}'.", [nameof(UpPartyType), nameof(UpPartyName)]));
                            }
                            break;
                        default:
                            throw new NotSupportedException($"Claim transformation task action '{TaskAction}' is not supported with type '{Type}' and action '{Action}'.");
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

                    switch (TaskAction)
                    {
                        case ClaimTransformTaskActions.QueryInternalUser:
                            if (TransformationExtension.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field {nameof(TransformationExtension)} is required for claim transformation task action '{TaskAction}'.", [nameof(TransformationExtension)]));
                            }
                            break;
                        case ClaimTransformTaskActions.QueryExternalUser:
                            if (UpPartyName.IsNullOrWhiteSpace() || TransformationExtension.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The fields {nameof(UpPartyName)} and {nameof(TransformationExtension)} is required for claim transformation task action '{TaskAction}'.", [nameof(UpPartyName), nameof(TransformationExtension)]));
                            }
                            break;
                        default:
                            throw new NotSupportedException($"Claim transformation task action '{TaskAction}' is not supported with type '{Type}' and action '{Action}'.");
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
                                else if (!ApiUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                {
                                    results.Add(new ValidationResult($"The field '{nameof(ApiUrl)}' is required to start with HTTPS.", [nameof(ApiUrl)]));
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
            if (TaskAction == null && Transformation.IsNullOrWhiteSpace())
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
            if (TaskAction == null && TransformationExtension.IsNullOrWhiteSpace())
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
