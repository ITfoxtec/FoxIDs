using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public abstract class LoginBaseViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool ShowCancelLogin { get; set; }

        public bool ShowPasswordAuth { get; set; }

        public bool ShowPasswordlessEmail { get; set; }

        public bool ShowPasswordlessSms { get; set; }

        public bool ShowSetPassword { get; set; }

        public bool ShowCreateUser { get; set; }

        public bool EnableChangeUserIdentifier { get; set; }

        [ValidateComplexType]
        public EmailPasswordViewModel EmailIdentifier { get; set; }

        [ValidateComplexType]
        public PhonePasswordViewModel PhoneIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePasswordViewModel UsernameIdentifier { get; set; }

        [ValidateComplexType]
        public UsernameEmailPasswordViewModel UsernameEmailIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePhonePasswordViewModel UsernamePhoneIdentifier { get; set; }

        [ValidateComplexType]
        public PhoneEmailPasswordViewModel PhoneEmailIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePhoneEmailPasswordViewModel UsernamePhoneEmailIdentifier { get; set; }

        public List<DynamicElementBase> Elements { get; set; } = new List<DynamicElementBase>();
    }
}
