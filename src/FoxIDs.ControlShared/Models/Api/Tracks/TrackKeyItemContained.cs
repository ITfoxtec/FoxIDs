using ITfoxtec.Identity.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Track key item containing a JSON Web Key.
    /// </summary>
    public class TrackKeyItemContained
    {
        /// <summary>
        /// Signing key material.
        /// </summary>
        [Required]
        public JsonWebKey Key { get; set; }
    }
}
