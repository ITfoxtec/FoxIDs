﻿using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthDownScope
    {
        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.ScopeLength)]
        [RegularExpression(Constants.Models.OAuthDownParty.ScopeRegExPattern)]
        [Display(Name = "Scope")]
        public string Scope { get; set; }

        [ListLength(Constants.Models.OAuthDownParty.Client.VoluntaryClaimsMin, Constants.Models.OAuthDownParty.Client.VoluntaryClaimsMax)]
        [Display(Name = "Voluntary claims")]
        public List<OAuthDownClaim> VoluntaryClaims { get; set; }
    }
}
