using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class RiskPasswordDelete
    {
        [ListLength(1, 10000)]
        public List<string> PasswordSha1Hashs { get; set; }
    }
}
