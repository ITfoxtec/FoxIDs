using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DownPartyTestStartResponse
    {
        [Required]
        [Display(Name = "Test URL")]
        public string TestUrl { get; set; }

        /// <summary>
        /// Test expiration time.
        /// </summary>
        public DateTime ExpireAt { get; set; }
    }
}
