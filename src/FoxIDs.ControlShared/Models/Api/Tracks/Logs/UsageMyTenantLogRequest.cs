using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    // Used to query usage logs in my tenant.
    public class UsageMyTenantLogRequest : UsageLogRequest
    {
        /// <summary>
        /// Select by full track name. Only possible in master track.
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string TrackName { get; set; }
    }
}
