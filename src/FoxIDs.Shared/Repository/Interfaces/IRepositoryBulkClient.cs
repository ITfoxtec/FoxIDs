using Microsoft.Azure.Cosmos;

namespace FoxIDs.Repository
{
    public interface IRepositoryBulkClient
    {
        CosmosClient Client { get; }
        Container Container { get; }
    }
}