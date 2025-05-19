using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class UsersRequest
    {
        [ListLength(1, 10000)]
        public List<CreateUserRequest> Users { get; set; }
    }
}
