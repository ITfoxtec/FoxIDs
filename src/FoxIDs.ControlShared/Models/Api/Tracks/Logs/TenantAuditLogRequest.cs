using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Tenant request payload for querying audit logs.
    /// </summary>
    public class TenantAuditLogRequest : AuditLogRequest
    {
        /// <summary>
        /// Optional tenant filter. Defaults to the route tenant when omitted.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        public string TenantName { get; set; }

        /// <summary>
        /// Optional track filter. Defaults to the route track when omitted.
        /// </summary>
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string TrackName { get; set; }  
    }
}
