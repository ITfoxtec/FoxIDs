using Microsoft.Azure.Cosmos;

namespace FoxIDs.Repository
{
    public interface ICosmosDbDataRepositoryBulkClient
    {
        CosmosClient Client { get; }
        Container Container { get; }
    }
}