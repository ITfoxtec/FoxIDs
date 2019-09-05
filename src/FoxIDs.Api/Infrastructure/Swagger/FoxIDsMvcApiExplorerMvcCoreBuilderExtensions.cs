using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoxIDs.Infrastructure.Swagger
{
    public static class FoxIDsMvcApiExplorerMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddFoxIDsApiExplorer(this IMvcCoreBuilder builder)
        {
            builder.Services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, FoxIDsApiDescriptionGroupCollectionProvider>();
            builder.Services.TryAddEnumerable(
                //ServiceDescriptor.Transient<IApiDescriptionProvider, FoxIDsDefaultApiDescriptionProvider>());
                ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());

            return builder;
        }
    }
}
