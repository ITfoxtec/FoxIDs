using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class RiskPassword
    {
        [Length(1, 1000)]
        public List<RiskPasswordItem> RiskPasswords { get; set; }
    }
}
