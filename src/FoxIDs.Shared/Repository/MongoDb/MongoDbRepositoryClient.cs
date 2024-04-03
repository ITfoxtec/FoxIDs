using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FoxIDs.Repository.MongoDb
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
            var database = mongoClient.GetDatabase(settings.MongoDb.DatabaseName);

            InitCollection<AuthCodeTtlGrant>(database);
            InitCollection<RefreshTokenTtlGrant>(database);
            InitCollection<CacheTtlData>(database);
        }

        private void InitCollection<T>(IMongoDatabase database)
        {
            var name = GetCollectionName<T>();
            database.CreateCollection(name);
            var collection = database.GetCollection<T>(name);
            if (typeof(T) is IDataTtlDocument)
            {
                var indexModel = new CreateIndexModel<T>(keys: Builders<T>.IndexKeys.Ascending("expire_at"),
                    options: new CreateIndexOptions
                    {
                        ExpireAfter = TimeSpan.FromSeconds(0),
                        Name = $"{name}_ExpireAtIndex"
                    });
                collection.Indexes.CreateOne(indexModel);
            }
        }

        public IMongoCollection<T> GetCollection<T>()
        {

            var database = mongoClient.GetDatabase(settings.MongoDb.DatabaseName);
            return database.GetCollection<T>(GetCollectionName<T>());
        }

        private string GetCollectionName<T>()
        {
            var name = typeof(T).Name;
            if (name.EndsWith("Party", StringComparison.OrdinalIgnoreCase))
            {
                name = $"{name.Substring(name.Length - 5)}Parties";
            }
            else if (!name.EndsWith("Data", StringComparison.OrdinalIgnoreCase))
            {
                name = $"{name}s";
            }

            return name;
        }
    }
}
