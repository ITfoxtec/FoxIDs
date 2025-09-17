using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class IdentifierViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        public bool EnableCreateUser { get; set; }

        [ValidateComplexType]
        public EmailIdentifierViewModel EmailIdentifier { get; set; }

        [ValidateComplexType]
        public PhoneIdentifierViewModel PhoneIdentifier { get; set; }

        [ValidateComplexType]
        public UsernameIdentifierViewModel UsernameIdentifier { get; set; }

        [ValidateComplexType]
        public UsernameEmailIdentifierViewModel UsernameEmailIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePhoneIdentifierViewModel UsernamePhoneIdentifier { get; set; }

        [ValidateComplexType]
        public PhoneEmailIdentifierViewModel PhoneEmailIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePhoneEmailIdentifierViewModel UsernamePhoneEmailIdentifier { get; set; }

        public bool ShowUserIdentifierSelection { get; set; }

        public List<DynamicElementBase> Elements { get; set; } = new List<DynamicElementBase>();

        [Display(Name = "Search log in")]
        public string UpPartyFilter { get; set; }

        public IEnumerable<IdentifierUpPartyViewModel> UpPatries { get; set; }
    }
}