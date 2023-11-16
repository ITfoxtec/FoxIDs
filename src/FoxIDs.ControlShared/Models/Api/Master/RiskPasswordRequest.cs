using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class RiskPasswordRequest
    {
        [ListLength(1, 10000)]
        public List<RiskPassword> RiskPasswords { get; set; }
    }
}
