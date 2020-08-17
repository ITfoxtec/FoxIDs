using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OAuthDownPartyViewModel
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Down Party name")]
        public string Name { get; set; }

        /// <summary>
        /// OAuth 2.0 down resource.
        /// </summary>
        [Required]
        public OAuthDownResourceViewModel Resource { get; set; }

        /// <summary>
        /// Allow cors origins.
        /// </summary>
        [Length(Constants.Models.OAuthDownParty.AllowCorsOriginsMin, Constants.Models.OAuthDownParty.AllowCorsOriginsMax, Constants.Models.OAuthDownParty.AllowCorsOriginLength)]
        [Display(Name = "Allow cors origins")]
        public List<string> AllowCorsOrigins { get; set; }
    }
}
