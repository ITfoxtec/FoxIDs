using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ClaimTransformViewModel : IValidatableObject
    {
        [MaxLength(Constants.Models.Claim.TransformNameLength)]
        [RegularExpression(Constants.Models.Claim.TransformNameRegExPattern)]
        public string Name { get; set; }

        [Required]
        public ClaimTransformTypes Type { get; set; }

        [Range(Constants.Models.Claim.TransformOrderMin, Constants.Models.Claim.TransformOrderMax)]
        public int Order { get; set; }

        [Display(Name = "Select claim")]
        public virtual string ClaimIn { get; set; }

        [Display(Name = "Select claims")]
        public virtual List<string> ClaimsIn { get; set; }

        public virtual string ClaimOut { get; set; }

        [Required]
        [Display(Name = "Action")]
        public ClaimTransformActions Action { get; set; } = ClaimTransformActions.Add;

        [Display(Name = "Transformation")]
        public virtual string Transformation { get; set; }

        [Display(Name = "Transformation extension")]
        public virtual string TransformationExtension { get; set; }

        [Display(Name = "External connect type")]
        public ExternalConnectTypes? ExternalConnectType { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Secret")]
        public string Secret { get; set; }

        public bool ShowDetails { get; set; }

        public string SecretLoaded { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Type != ClaimTransformTypes.ExternalClaims && ClaimOut.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field is required.", [nameof(ClaimOut)]));
            }

            if (Action == ClaimTransformActions.Add || Action == ClaimTransformActions.Replace)
            {
                switch (Type)
                {
                    case ClaimTransformTypes.Constant:
                        if (Transformation.IsNullOrWhiteSpace())
                        {
                            results.Add(new ValidationResult($"The field is required.", [nameof(Transformation)]));
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
                        break;

                    case ClaimTransformTypes.RegexMap:
                        ValidateRegexMapClaimAddReplace(results);
                        break;

                    case ClaimTransformTypes.Concatenate:
                        if (Transformation.IsNullOrWhiteSpace())
                        {
                            results.Add(new ValidationResult($"The field is required.", [nameof(Transformation)]));
                        }
                        break;
                    case ClaimTransformTypes.ExternalClaims:
                        if (Name.IsNullOrWhiteSpace())
                        {
                            results.Add(new ValidationResult($"The field is required.", [nameof(Name)]));
                        }
                        if (ExternalConnectType == ExternalConnectTypes.Api)
                        {
                            if (ApiUrl.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field is required.", [nameof(ApiUrl)]));
                            }
                            if (Secret.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field is required.", [nameof(Secret)]));
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
                            results.Add(new ValidationResult($"The field is required.", [nameof(Transformation)]));
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
            if (Transformation.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field is required.", [nameof(Transformation)]));
            }
        }

        private void ValidateMatchAddReplace(List<ValidationResult> results)
        {
            if (Transformation.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field is required.", [nameof(Transformation)]));
            }
            if (TransformationExtension.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field is required.", [nameof(TransformationExtension)]));
            }
        }

        private void ValidateRegexMapClaimAddReplace(List<ValidationResult> results)
        {
            if (Transformation.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field is required.", [nameof(Transformation)]));
            }
        }
    }
}

