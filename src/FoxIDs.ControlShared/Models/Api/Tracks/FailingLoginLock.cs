using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class FailingLoginLock 
    {
        [Required]
        [MaxLength(Constants.Models.FailingLoginLock.UserIdentifierLength)]
        [RegularExpression(Constants.Models.FailingLoginLock.UserIdentifierRegExPattern)]
        [Display(Name = "User identifier")]
        public string UserIdentifier { get; set; }

        [Required]
        [Display(Name = "Failing login type")]
        public FailingLoginTypes FailingLoginType { get; set; }

        [Required]
        [Display(Name = "Create time")]
        public long CreateTime { get; set; }

        [Required]
        [Display(Name = "Time to live")]
        public int TimeToLive { get; set; }
    }
}
