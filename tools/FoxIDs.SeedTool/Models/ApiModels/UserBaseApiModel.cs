using System.Collections.Generic;

namespace FoxIDs.SeedTool.Models.ApiModels
{
    public class UserBaseApiModel
    {
        public string Email { get; set; }

        public string Phone { get; set; }

        public string Username { get; set; }

        public bool ConfirmAccount { get; set; }

        public bool EmailVerified { get; set; }

        public bool PhoneVerified { get; set; }

        public bool ChangePassword { get; set; }

        public bool SetPasswordEmail { get; set; }

        public bool SetPasswordSms { get; set; }

        public bool DisableAccount { get; set; }

        public bool DisableTwoFactorApp { get; set; }

        public bool DisableTwoFactorSms { get; set; }

        public bool DisableTwoFactorEmail { get; set; }

        public bool ActiveTwoFactorApp { get; set; }

        public bool RequireMultiFactor { get; set; }

        public List<ClaimAndValuesApiModel> Claims { get; set; }
    }
}
