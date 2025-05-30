﻿using FoxIDs.Models.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class RefreshTokenGrantViewModel : RefreshTokenGrant
    {
        [Display(Name = "Expire at")]
        public string ExpireAtText
        {
            get
            {
                return TimeToLive.HasValue ? DateTimeOffset.FromUnixTimeSeconds(CreateTime + TimeToLive.Value).ToUniversalTime().ToLocalTime().ToString() : string.Empty;
            }
            set { }
        }
    }
}
