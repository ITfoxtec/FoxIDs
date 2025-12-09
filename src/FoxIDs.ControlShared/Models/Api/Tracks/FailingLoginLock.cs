using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Represents a lock placed after repeated failing login attempts.
    /// </summary>
    public class FailingLoginLock 
    {
        /// <summary>
        /// Identifier used to track the locked user.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.FailingLoginLock.UserIdentifierLength)]
        [RegularExpression(Constants.Models.FailingLoginLock.UserIdentifierRegExPattern)]
        [Display(Name = "User identifier")]
        public string UserIdentifier { get; set; }

        /// <summary>
        /// Login flow that triggered the lock.
        /// </summary>
        [Required]
        [Display(Name = "Failing login type")]
        public FailingLoginTypes FailingLoginType { get; set; }

        /// <summary>
        /// Time the lock was created (Unix seconds).
        /// </summary>
        [Required]
        [Display(Name = "Create time")]
        public long CreateTime { get; set; }

        /// <summary>
        /// Duration the lock remains active (seconds).
        /// </summary>
        [Required]
        [Display(Name = "Time to live")]
        public int TimeToLive { get; set; }
    }
}
