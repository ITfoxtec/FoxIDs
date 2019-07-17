using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Model
{
    public class RiskPasswordApiModel
    {
        [Length(1, 1000)]
        public List<RiskPasswordItemApiModel> RiskPasswords { get; set; }
    }
}
