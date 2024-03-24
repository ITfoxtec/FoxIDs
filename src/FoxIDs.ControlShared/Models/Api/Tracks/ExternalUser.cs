using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ExternalUser : ExternalUserId
    {
        /// <summary>
        /// User id (unique and persistent).
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        public string UserId { get; set; }

        public bool DisableAccount { get; set; }

        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
