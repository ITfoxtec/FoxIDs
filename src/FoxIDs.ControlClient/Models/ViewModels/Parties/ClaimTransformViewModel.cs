using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ClaimTransformViewModel : IValidatableObject, IUpPartySelection
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
        public ClaimTransformActions Action { get; set; } = ClaimTransformActions.Replace;

        [Display(Name = "Transformation")]
        public virtual string Transformation { get; set; }

        [Display(Name = "Transformation extension")]
        public virtual string TransformationExtension { get; set; }

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

        #region Task
        [Display(Name = "Task")]
        public ClaimTransformTasks? Task { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Error")]
        public string Error { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Error message")]
        public string ErrorMessage { get; set; }

        [Display(Name = "Authentication type")]
        public PartyTypes? UpPartyType { get; set; }

        public string UpPartyTypeText { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string UpPartyName { get; set; }

        [Display(Name = "Authentication name")]
        public string UpPartyDisplayName { get; set; }

        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string UpPartyProfileName { get; set; }

        [Display(Name = "Authentication profile name")]
        public string UpPartyDisplayProfileName { get; set; }
        #endregion

        public bool ShowDetails { get; set; }

        public string SecretLoaded { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Task == null && Type != ClaimTransformTypes.ExternalClaims && ClaimOut.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field is required.", [nameof(ClaimOut)]));
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
                            if (Error.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field is required.", [nameof(Error)]));
                            }
                            if (ErrorMessage.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field is required.", [nameof(ErrorMessage)]));
                            }
                            break;
                        case ClaimTransformTasks.UpPartyAction:
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
                        case ClaimTransformTasks.QueryExternalUser:
                            if (Transformation.IsNullOrWhiteSpace())
                            {
                                results.Add(new ValidationResult($"The field is required.", [nameof(Transformation)]));
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
            }
            return results;
        }

        private void ValidateMatchClaimAddReplace(List<ValidationResult> results)
        {
            if (Task == null && Transformation.IsNullOrWhiteSpace())
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
            if (Task == null && TransformationExtension.IsNullOrWhiteSpace())
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