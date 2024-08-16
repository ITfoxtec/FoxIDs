using OpenSearch.Client;

namespace FoxIDs.Infrastructure.Hosting
{
    public class OpenSearchClientQueryLog : OpenSearchClient
    {
        public OpenSearchClientQueryLog(IConnectionSettingsValues connectionSettings) : base(connectionSettings)
        { }
    }
}
