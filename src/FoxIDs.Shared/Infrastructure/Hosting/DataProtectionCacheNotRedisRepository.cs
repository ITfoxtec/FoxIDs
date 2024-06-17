using FoxIDs.Logic.Caches.Providers;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FoxIDs.Infrastructure.Hosting
{
    public class DataProtectionCacheNotRedisRepository : IXmlRepository
    {
        private readonly IDataCacheProvider cacheProvider;
        private string key;

        public DataProtectionCacheNotRedisRepository(IDataCacheProvider cacheProvider, string key)
        {
            this.cacheProvider = cacheProvider;
            this.key = key;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return GetAllElementsCore().ToList().AsReadOnly();
        }

        private IEnumerable<XElement> GetAllElementsCore()
        {
            cacheProvider.GetAsync

            var database = _databaseFactory();
            foreach (var value in database.ListRange(key))
            {
                yield return XElement.Parse((string)value!);
            }
        }

        /// <inheritdoc />
        public void StoreElement(XElement element, string friendlyName)
        {
            var database = _databaseFactory();
            database.ListRightPush(key, element.ToString(SaveOptions.DisableFormatting));
        }
    }
}
