using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DownPartyTestStartResponse
    {
        /// <summary>
        /// Test application name.
        /// </summary>
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [Required]
        [MaxLength(Constants.Models.DownParty.UrlLengthMax)]
        [Display(Name = "Test URL")]
        public string TestUrl { get; set; }

        /// <summary>
        /// Test expiration time in Unix time seconds.
        /// </summary>
        public long TestExpireAt { get; set; }
    }
}
