using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Summary information about stored risky passwords.
    /// </summary>
    public class RiskPasswordInfo
    {
        /// <summary>
        /// Count of risky password hashes stored.
        /// </summary>
        [Display(Name = "Risk password count")]
        public long? RiskPasswordCount { get; set; }
    }
}
