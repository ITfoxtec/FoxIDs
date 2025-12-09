using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to save or update risky password hashes.
    /// </summary>
    public class RiskPasswordRequest
    {
        /// <summary>
        /// Collection of risky password hashes and counts.
        /// </summary>
        [ListLength(Constants.ControlApi.SaveAndDeleteManyMin, Constants.ControlApi.SaveAndDeleteManyMax)]
        public List<RiskPassword> RiskPasswords { get; set; }
    }
}
