using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Bulk delete users request.
    /// </summary>
    public class UsersDelete
    {
        [ListLength(Constants.ControlApi.SaveAndDeleteManyMin, Constants.ControlApi.SaveAndDeleteManyMax)]
        public List<string> UserIdentifiers { get; set; }
    }
}
