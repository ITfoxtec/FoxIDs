using FoxIDs.Models.Config;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class DataProtectionBuilderExtension
    {
        public static IDataProtectionBuilder PersistKeysToCacheNotRedis(this IDataProtectionBuilder builder, CacheOptions cache, string key)
        {
            builder.Services.Configure<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new DataProtectionCacheNotRedisRepository(key);
            });
            return builder;
        }
    }
}
