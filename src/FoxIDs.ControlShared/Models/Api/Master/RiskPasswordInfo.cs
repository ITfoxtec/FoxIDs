using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class RiskPasswordInfo
    {
        [Display(Name = "Risk password count")]
        public int? RiskPasswordCount { get; set; }
    }
}
