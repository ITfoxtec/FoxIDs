using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    // Used to query usage logs in tenant.
    public class UsageTenantLogRequest : UsageLogRequest
    {
        /// <summary>
        /// Select by full tenant name.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        public string TenantName { get; set; }

        /// <summary>
        /// Select by full track name. 
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string TrackName { get; set; }
    }
}
