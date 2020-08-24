using Microsoft.Azure.Cosmos;

namespace FoxIDs.Repository
{
    public interface IRepositoryCosmosClient
    {
        CosmosClient CosmosClient { get; }

        Container Container { get; }
    }
}