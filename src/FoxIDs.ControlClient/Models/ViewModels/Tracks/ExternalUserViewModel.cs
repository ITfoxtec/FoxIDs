﻿using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ExternalUserViewModel 
    {
        public ExternalUserViewModel()
        {
            Claims = new List<ClaimAndValues>();
        }

        [Required(ErrorMessage = "Select which authentication method the external user must be assigned to.")]
        [MaxLength(Constants.Models.Party.NameLength)]
        [Display(Name = "Connected authentication method")]
        public string UpPartyName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [Display(Name = "Connected authentication method")]
        public string UpPartyDisplayName { get; set; }

        [Required(ErrorMessage = "A unique claim value is required.")]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Link claim value")]
        public string LinkClaimValue { get; set; }

        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [Display(Name = "Account status")]
        public bool DisableAccount { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [Display(Name = "Claims")]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
