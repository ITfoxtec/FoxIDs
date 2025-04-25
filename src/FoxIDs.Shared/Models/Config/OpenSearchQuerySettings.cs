namespace FoxIDs.Models.Config
{
    public class OpenSearchQuerySettings : OpenSearchBaseSettings
    {
        /// <summary>
        /// Optional cross-cluster search, external cluster name
        /// </summary>
        public string CrossClusterSearchClusterName { get; set; }
    }
}
