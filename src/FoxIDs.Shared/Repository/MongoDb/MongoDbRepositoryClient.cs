using FoxIDs.Models;
using FoxIDs.Models.Config;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class MongoDbRepositoryClient
    {
        private readonly Settings settings;
        private readonly IMongoClient mongoClient;

        public MongoDbRepositoryClient(Settings settings, IMongoClient mongoClient)
        {
            this.settings = settings;
            this.mongoClient = mongoClient;

            InitRegistry();
        }

        private void InitRegistry()
        {
            var pack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new IgnoreIfNullConvention(true),
                new MongoDbJsonPropertyConvention()
            };
            ConventionRegistry.Register(nameof(MongoDbRepositoryClient), pack, t => true);
        }

        public async Task InitAsync(CancellationToken cancellationToken = default)
        {
            var database = mongoClient.GetDatabase(settings.MongoDb.DatabaseName);

            if (settings.Options.DataStorage == DataStorageOptions.MongoDb)
            {
                _ = await InitCollectionAsync<DataDocument>(database, settings.MongoDb.MasterCollectionName, cancellationToken);
                await InitTtlCollectionAsync<DataTtlDocument>(database, settings.MongoDb.MasterTtlCollectionName, cancellationToken);

                _ = await InitCollectionAsync<DataDocument>(database, settings.MongoDb.TenantsCollectionName, cancellationToken);
                await InitTtlCollectionAsync<DataTtlDocument>(database, settings.MongoDb.TenantsTtlCollectionName, cancellationToken);
            }
            if (settings.Options.Cache == CacheOptions.MongoDb)
            {
                await InitTtlCollectionAsync<DataTtlDocument>(database, settings.MongoDb.CacheCollectionName, cancellationToken);
            }
        }

        private async Task<IMongoCollection<T>> InitCollectionAsync<T>(IMongoDatabase database, string name, CancellationToken cancellationToken) where T : DataDocument
        {
            await database.CreateCollectionAsync(name);

            var collection = database.GetCollection<T>(name);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending(f => f.PartitionId)), cancellationToken: cancellationToken);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending(f => f.DataType)), cancellationToken: cancellationToken);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending(f => f.AdditionalIds),
                options: new CreateIndexOptions
                {
                    Unique = true,
                    Sparse = true,
                }), cancellationToken: cancellationToken);
            return collection;
        }

        private async Task InitTtlCollectionAsync<T>(IMongoDatabase database, string name, CancellationToken cancellationToken) where T : DataTtlDocument
        {
            var collection = await InitCollectionAsync<T>(database, name, cancellationToken);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending(f => f.ExpireAt),
                options: new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.FromSeconds(0),
                    Name = $"{name}ExpireAtIndex"
                }), cancellationToken: cancellationToken);
        }

        public IMongoCollection<T> GetMasterCollection<T>(T item = default)
        {
            if (IsTtlDocument(item))
            {
                return GetCollection<T>(settings.MongoDb.MasterTtlCollectionName);
            }
            else
            {
                return GetCollection<T>(settings.MongoDb.MasterCollectionName);
            }
        }

        public IMongoCollection<T> GetTenantsCollection<T>(T item = default)
        {
            if (IsTtlDocument(item))
            {
                return GetCollection<T>(settings.MongoDb.TenantsTtlCollectionName);
            }
            else
            {
                return GetCollection<T>(settings.MongoDb.TenantsCollectionName);
            }
        }

        private static bool IsTtlDocument<T>(T item)
        {
            if (item != null)
            {
                return item.GetType().GetInterface(nameof(IDataTtlDocument)) != null;
            }
            else
            {
                return typeof(T).GetInterface(nameof(IDataTtlDocument)) != null;
            }
        }

        public IMongoCollection<T> GetCacheCollection<T>()
        {
            return GetCollection<T>(settings.MongoDb.CacheCollectionName);
        }

        private IMongoCollection<T> GetCollection<T>(string name)
        {
            var database = mongoClient.GetDatabase(settings.MongoDb.DatabaseName);
            return database.GetCollection<T>(name);
        }
    }
}
