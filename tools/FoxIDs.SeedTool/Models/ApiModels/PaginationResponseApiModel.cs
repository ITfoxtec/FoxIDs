using System.Collections.Generic;

namespace FoxIDs.SeedTool.Models.ApiModels
{
    public class PaginationResponse<Tdata>
    {
        public HashSet<Tdata> Data { get; set; }

        public string PaginationToken { get; set; }

        public int Limit { get; set; } = Constants.Models.ListPageSize;
    }
}
