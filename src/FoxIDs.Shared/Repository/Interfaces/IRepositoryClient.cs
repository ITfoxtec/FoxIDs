using Microsoft.Azure.Cosmos;

namespace FoxIDs.Repository
{
    public interface IRepositoryClient
    {
        CosmosClient Client { get; }
        Container Container { get; }
        Container TtlContainer { get; }
    }
}