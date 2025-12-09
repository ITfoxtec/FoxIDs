using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to delete risky password hashes.
    /// </summary>
    public class RiskPasswordDelete
    {
        /// <summary>
        /// SHA1 hashes to remove from the risky password store.
        /// </summary>
        [ListLength(Constants.ControlApi.SaveAndDeleteManyMin, Constants.ControlApi.SaveAndDeleteManyMax)]
        public List<string> PasswordSha1Hashs { get; set; }
    }
}
