using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ExternalUserUpdateRequest : ExternalUserRequest
    {
        /// <summary>
        /// Add a value to change which authentication method (up-party) the external user is connected to.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        public string UpdateUpPartyName { get; set; }

        /// <summary>
        /// Add a value to change the link claim value. The field is set to an empty string if the value should be removed.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string UpdateLinkClaimValue { get; set; }

        /// <summary>
        /// Add a value to change the redemption claim value. The field is set to an empty string if the value should be removed.
        /// </summary>
        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string UpdateRedemptionClaimValue { get; set; }
    }
}
