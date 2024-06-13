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
        /// Test expiration time in Unix time seconds.
        /// </summary>
        public long ExpireAt { get; set; }
    }
}
