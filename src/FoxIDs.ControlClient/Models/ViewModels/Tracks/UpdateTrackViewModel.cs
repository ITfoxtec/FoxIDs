using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UpdateTrackViewModel : CreateTrackViewModel
    {
        public UpdateTrackViewModel()
        {
            AllowIframeOnDomains = new List<string>();
        }

        [Range(Constants.Models.Track.SequenceLifetimeMin, Constants.Models.Track.SequenceLifetimeMax)] 
        [Display(Name = "Sequence lifetime")]
        public int SequenceLifetime { get; set; } 

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password min length")]
        public int PasswordLength { get; set; } 

        [Required]
        [Display(Name = "Check password complexity")]
        public bool? CheckPasswordComplexity { get; set; }

        [Required]
        [Display(Name = "Check password risk")]
        public bool? CheckPasswordRisk { get; set; } 

        [ValidateComplexType]
        [Length(Constants.Models.Track.AllowIframeOnDomainsMin, Constants.Models.Track.AllowIframeOnDomainsMax, Constants.Models.Track.AllowIframeOnDomainsLength)]
        [Display(Name = "Allow Iframe on domains")]
        public List<string> AllowIframeOnDomains { get; set; }
    }
}
