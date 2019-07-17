using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static T BindConfig<T>(this IServiceCollection services, IConfiguration configuration, string key) where T : class, new()
        {
            var settings = new T();
            configuration.Bind(key, settings);
            services.AddSingleton(settings);

            return settings;
        }
    }
}
