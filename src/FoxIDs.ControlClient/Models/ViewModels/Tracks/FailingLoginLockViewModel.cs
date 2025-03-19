using FoxIDs.Models.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class FailingLoginLockViewModel : FailingLoginLock
    {
        public string Error { get; set; }

        [Display(Name = "Expire at")]
        public string ExpireAtText
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(CreateTime + TimeToLive).ToUniversalTime().ToLocalTime().ToString();
            }
            set { }
        }
    }
}
