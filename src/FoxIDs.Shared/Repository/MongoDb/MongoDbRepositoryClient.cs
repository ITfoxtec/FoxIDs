using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;

namespace FoxIDs.Repository
{
    public class MongoDbRepositoryClient
    {
        private readonly TelemetryLogger logger;
        private readonly Settings settings;
        private readonly IMongoClient mongoClient;

        public MongoDbRepositoryClient(TelemetryLogger logger, Settings settings, IMongoClient mongoClient)
        {
            this.logger = logger;
            this.settings = settings;
            this.mongoClient = mongoClient;
            Init();
        }

        private void Init()
        {
            var pack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new IgnoreIfNullConvention(true),
                new MongoDbJsonPropertyConvention()
            };
            ConventionRegistry.Register(nameof(MongoDbRepositoryClient), pack, t => true);

            //Mongo CamelCaseElementNameConvention for DateOnly do not work in DateOnly "MongoDB.Driver" version 3.0.0.
            var camelCaseConventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("camelCase", camelCaseConventionPack, t => t == typeof(DateOnly));

            var database = mongoClient.GetDatabase(settings.MongoDb.DatabaseName);

            if (settings.Options.DataStorage == DataStorageOptions.MongoDb)
            {
                _ = InitCollection<DataDocument>(database, settings.MongoDb.MasterCollectionName);
                InitTtlCollection<DataTtlDocument>(database, settings.MongoDb.MasterTtlCollectionName);

                _ = InitCollection<DataDocument>(database, settings.MongoDb.TenantsCollectionName);
                InitTtlCollection<DataTtlDocument>(database, settings.MongoDb.TenantsTtlCollectionName);
            }
            if (settings.Options.Cache == CacheOptions.MongoDb)
            {
                InitTtlCollection<DataTtlDocument>(database, settings.MongoDb.CacheCollectionName);
            }
        }

        private IMongoCollection<T> InitCollection<T>(IMongoDatabase database, string name) where T : DataDocument
        {
            database.CreateCollection(name);

            var collection = database.GetCollection<T>(name);
            collection.Indexes.CreateOne(new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending(f => f.PartitionId)));
            collection.Indexes.CreateOne(new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending(f => f.DataType)));
            collection.Indexes.CreateOne(new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending(f => f.AdditionalIds), 
                options: new CreateIndexOptions
                {
                    Unique = true,
                    Sparse = true,
                }));
            return collection;
        }

        private void InitTtlCollection<T>(IMongoDatabase database, string name) where T : DataTtlDocument
        {
            var collection = InitCollection<T>(database, name);
            collection.Indexes.CreateOne(new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending(f => f.ExpireAt),
                options: new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.FromSeconds(0),
                    Name = $"{name}ExpireAtIndex"
                }));
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
