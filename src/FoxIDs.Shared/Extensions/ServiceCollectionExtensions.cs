using FoxIDs;
using FoxIDs.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static T BindConfig<T>(this IServiceCollection services, IConfiguration configuration, string key) where T : class, new()
        {
            var settings = new T();
            configuration.Bind(key, settings);
            try
            {
                settings.ValidateObjectAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new InvalidConfigException(typeof(T).Name, ex); 
            }

            services.AddSingleton(settings);
            return settings;
        }
    }
}
