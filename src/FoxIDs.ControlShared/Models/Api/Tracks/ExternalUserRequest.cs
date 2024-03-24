using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class ExternalUserRequest : ExternalUserId
    {
        public bool DisableAccount { get; set; }

        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]       
        public List<ClaimAndValues> Claims { get; set; }
    }
}
