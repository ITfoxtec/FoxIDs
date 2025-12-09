using ITfoxtec.Identity;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request model used to fetch test results for a down-party authorization flow.
    /// </summary>
    public class DownPartyTestResultRequest
    {
        /// <summary>
        /// Correlation state returned from the authorization response.
        /// </summary>
        [Required]
        [MaxLength(IdentityConstants.MessageLength.StateMax)]
        [Display(Name = "State")]
        public string State { get; set; }

        /// <summary>
        /// Authorization code issued during the test flow.
        /// </summary>
        [Required]
        [MaxLength(IdentityConstants.MessageLength.CodeMax)]
        [Display(Name = "Code")]
        public string Code { get; set; }
    }
}
