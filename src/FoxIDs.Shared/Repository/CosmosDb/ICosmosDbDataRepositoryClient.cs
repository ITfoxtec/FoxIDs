using Microsoft.Azure.Cosmos;

namespace FoxIDs.Repository
{
    public interface ICosmosDbDataRepositoryClient
    {
        CosmosClient Client { get; }
        Container Container { get; }
        Container TtlContainer { get; }
    }
}