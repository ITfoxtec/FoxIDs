using System.Collections.Generic;
using System.Security.Claims;

namespace FoxIDs.Models.Logic
{
    public class CreateUserObj
    {
        public UserIdentifier UserIdentifier { get; set; }

        public string Password { get; set; }

        public bool ChangePassword { get; set; }

        public bool DisableSetPasswordSms { get; set; }

        public bool DisableSetPasswordEmail { get; set; }

        public bool SetPasswordSms { get; set; }

        public bool SetPasswordEmail { get; set; }

        public bool ConfirmAccount { get; set; } = true;

        public bool EmailVerified { get; set; }

        public bool PhoneVerified { get; set; }

        public bool DisableAccount { get; set; }

        public bool DisableTwoFactorApp { get; set; }

        public bool DisableTwoFactorSms { get; set; }

        public bool DisableTwoFactorEmail { get; set; }

        public string TwoFactorAppSecret { get; set; }

        public bool RequireMultiFactor { get; set; }

        public List<Claim> Claims { get; set; }
    }
}