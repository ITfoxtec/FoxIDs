using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Wraps paginated results returned from list endpoints.
    /// </summary>
    public class PaginationResponse<Tdata>
    {
        /// <summary>
        /// Current page of data.
        /// </summary>
        public HashSet<Tdata> Data { get; set; }

        /// <summary>
        /// Token to retrieve the next page.
        /// </summary>
        public string PaginationToken { get; set; }

        /// <summary>
        /// Maximum number of items returned.
        /// </summary>
        public int Limit { get; set; } = Constants.Models.ListPageSize;
    }
}
