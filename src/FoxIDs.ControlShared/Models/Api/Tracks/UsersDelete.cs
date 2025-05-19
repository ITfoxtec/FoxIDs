using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class UsersDelete
    {
        [ListLength(1, 10000)]
        public List<string> UserIdentifiers { get; set; }
    }
}
