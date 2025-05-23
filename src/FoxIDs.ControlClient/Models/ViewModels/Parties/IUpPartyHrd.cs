﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IUpPartyHrd
    {
        [Display(Name = "IP addresses and IP ranges")]
        public List<string> HrdIPAddressesAndRanges { get; set; }

        [Display(Name = "HRD Domains (use * to accept all domains not configured on another authentication method)")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Regular expressions")]
        public List<string> HrdRegularExpressions { get; set; }

        [Display(Name = "Show HRD button while using IP address / range, HRD domain or regular expression")]
        public bool HrdAlwaysShowButton { get; set; }

        [Display(Name = "Home Realm Discovery (HRD) Display name")]
        public string HrdDisplayName { get; set; }

        [Display(Name = "Home Realm Discovery (HRD) Logo URL")]
        public string HrdLogoUrl { get; set; }
    }
}
