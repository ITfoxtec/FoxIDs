using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    // Used to query logs in tenant.
    public class TenantLogRequest : LogRequest
    {
        /// <summary>
        /// Select by full tenant name.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        public string TenantName { get; set; }

        /// <summary>
        /// Select by full environment name. 
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string TrackName { get; set; }
    }
}
