using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class User : DataDocument, ISecretHash, IValidatableObject
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.User}:{idKey.TenantName}:{idKey.TrackName}:{(!idKey.Email.IsNullOrEmpty() ? idKey.Email : (!idKey.UserIdentifier.IsNullOrEmpty() ? idKey.UserIdentifier : idKey.UserId))}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, IdKey idKey)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await IdFormatAsync(new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                Email = idKey.Email,
                UserIdentifier = idKey.UserIdentifier,
                UserId = idKey.UserId,
            });
        }

        [Required]
        [MaxLength(Constants.Models.User.IdLength)]
        [RegularExpression(Constants.Models.User.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [ListLength(Constants.Models.User.AdditionalIdsMin, Constants.Models.User.AdditionalIdsMax, Constants.Models.User.IdLength, Constants.Models.User.IdRegExPattern)]
        [JsonProperty(PropertyName = "a_ids")]
        public override List<string> AdditionalIds { get; set; }

        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        [RegularExpression(Constants.Models.User.UserIdRegExPattern)]
        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashAlgorithmLength)]
        [JsonProperty(PropertyName = "hash_algorithm")]
        public string HashAlgorithm { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashLength)]
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [MaxLength(Constants.Models.SecretHash.HashSaltLength)]
        [JsonProperty(PropertyName = "hash_salt")]
        public string HashSalt { get; set; }

        [JsonProperty(PropertyName = "change_password")]
        public bool ChangePassword  { get; set; }

        /// <summary>
        /// SetPasswordEmail require the user to have a email user identifier.
        /// </summary>
        [JsonProperty(PropertyName = "set_password_email")]
        public bool SetPasswordEmail { get; set; }

        /// <summary>
        /// SetPasswordSms require the user to have a phone user identifier.
        /// </summary>
        [JsonProperty(PropertyName = "set_password_sms")]
        public bool SetPasswordSms { get; set; }

        [MaxLength(Constants.Models.User.EmailLength)]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [MaxLength(Constants.Models.User.PhoneLength)]
        [RegularExpression(Constants.Models.User.PhoneRegExPattern)]
        [JsonProperty(PropertyName = "phone")]
        public string Phone { get; set; }

        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }
       
        [JsonProperty(PropertyName = "confirm_account")]
        public bool ConfirmAccount { get; set; }

        [JsonProperty(PropertyName = "email_verified")]
        public bool EmailVerified { get; set; }

        [JsonProperty(PropertyName = "phone_verified")]
        public bool PhoneVerified { get; set; }

        [JsonProperty(PropertyName = "disable_account")]
        public bool DisableAccount { get; set; }

        [JsonProperty(PropertyName = "disable_two_factor_app")]
        public bool DisableTwoFactorApp { get; set; }

        [JsonProperty(PropertyName = "disable_two_factor_sms")]
        public bool DisableTwoFactorSms { get; set; }

        [JsonProperty(PropertyName = "disable_two_factor_email")]
        public bool DisableTwoFactorEmail { get; set; }

        [JsonProperty(PropertyName = "two_factor_app_secret")]
        public string TwoFactorAppSecret { get; set; }

        [JsonProperty(PropertyName = "two_factor_app_secret_external_name")]
        public string TwoFactorAppSecretExternalName { get; set; }

        [JsonProperty(PropertyName = "two_factor_app_recovery_code")]
        public TwoFactorAppRecoveryCode TwoFactorAppRecoveryCode { get; set; }

        [JsonProperty(PropertyName = "require_multi_factor")]
        public bool RequireMultiFactor { get; set; }

        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
        }
        
        public async Task SetAdditionalIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            if (AdditionalIds == null)
            {
                AdditionalIds = new List<string>();
            }

            AdditionalIds.Add(await IdFormatAsync(idKey));
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Email.IsNullOrEmpty() && Phone.IsNullOrEmpty() && Username.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"Either the field {nameof(Email)} or the field {nameof(Phone)} or the field {nameof(Username)} is required.", [nameof(Email), nameof(Phone), nameof(Username)]));
            }

            if (SetPasswordEmail)
            {
                if (Email.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Email)} is required to set password with email.", [nameof(Email), nameof(SetPasswordEmail)]));
                }
            }
            if (SetPasswordSms)
            {
                if (Phone.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Phone)} is required to set password with SMS.", [nameof(Phone), nameof(SetPasswordSms)]));
                }
            }

            if (RequireMultiFactor && DisableTwoFactorApp && DisableTwoFactorSms && DisableTwoFactorEmail)
            {
                results.Add(new ValidationResult($"Either the field {nameof(DisableTwoFactorApp)} or the field {nameof(DisableTwoFactorSms)} or the field {nameof(DisableTwoFactorEmail)} should be False if the field {nameof(RequireMultiFactor)} is True.",
                    [nameof(DisableTwoFactorApp), nameof(DisableTwoFactorSms), nameof(DisableTwoFactorEmail), nameof(RequireMultiFactor)]));
            }
            
            return results;
        }

        public class IdKey : Track.IdKey, IValidatableObject
        {
            [MaxLength(Constants.Models.User.EmailLength)]
            [RegularExpression(Constants.Models.User.EmailRegExPattern)]
            public string Email { get; set; }

            [MaxLength(Constants.Models.User.UsernameLength)]
            [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
            public string UserIdentifier { get; set; }

            [MaxLength(Constants.Models.User.UserIdLength)]
            [RegularExpression(Constants.Models.User.UserIdRegExPattern)]
            public string UserId { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                var results = new List<ValidationResult>();

                if (Email.IsNullOrEmpty() && UserIdentifier.IsNullOrEmpty() && UserId.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Either the field {nameof(Email)} or the field {nameof(UserIdentifier)} or the field {nameof(UserId)} is required.", [nameof(Email), nameof(UserIdentifier), nameof(UserId)]));
                }

                return results;
            }
        }
    }
}
