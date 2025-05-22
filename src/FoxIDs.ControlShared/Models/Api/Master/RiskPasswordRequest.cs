using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class RiskPasswordRequest
    {
        [ListLength(Constants.ControlApi.SaveAndDeleteManyMin, Constants.ControlApi.SaveAndDeleteManyMax)]
        public List<RiskPassword> RiskPasswords { get; set; }
    }
}
