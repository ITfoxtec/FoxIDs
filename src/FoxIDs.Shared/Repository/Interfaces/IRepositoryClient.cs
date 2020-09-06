using System;
using Microsoft.Azure.Documents.Client;

namespace FoxIDs.Repository
{
    public interface IRepositoryClient
    {
        DocumentClient Client { get; }
        string DatabaseId { get; }
        string CollectionId { get; }
        string TtlCollectionId { get; }
        public Uri DatabaseUri { get; }
        Uri CollectionUri { get; }
        Uri TtlCollectionUri { get; }
    }
}