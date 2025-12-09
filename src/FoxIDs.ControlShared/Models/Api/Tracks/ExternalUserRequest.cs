using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to synchronize an external user with optional claim data.
    /// </summary>
    public class ExternalUserRequest : ExternalUserId
    {
        /// <summary>
        /// Disable the account if set.
        /// </summary>
        public bool DisableAccount { get; set; }

        /// <summary>
        /// Claims to associate with the external user.
        /// </summary>
        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]       
        public List<ClaimAndValues> Claims { get; set; }
    }
}
