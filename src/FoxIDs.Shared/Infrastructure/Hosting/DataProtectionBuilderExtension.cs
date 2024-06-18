using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class DataProtectionBuilderExtension
    {
        public static IDataProtectionBuilder PersistKeysToGeneralRepository(this IDataProtectionBuilder builder)
        {
            builder.Services.AddOptions<KeyManagementOptions>()
                .Configure<IServiceScopeFactory>((options, factory) =>
                {
                    options.XmlRepository = new DataProtectionGeneralRepository(factory);
                });
            return builder;
        }
    }
}
