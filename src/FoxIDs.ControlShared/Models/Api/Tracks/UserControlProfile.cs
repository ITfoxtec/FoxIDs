using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserControlProfile
    {
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [Display(Name = "Last environment")]
        public string LastTrackName { get; set; }        
    }
}
