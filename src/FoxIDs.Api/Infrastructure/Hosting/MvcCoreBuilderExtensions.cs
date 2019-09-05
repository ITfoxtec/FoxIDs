using FoxIDs.Infrastructure.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class MvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddFoxIDsApiExplorer(this IMvcCoreBuilder builder)
        {
            builder.Services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, FoxIDsApiDescriptionGroupCollectionProvider>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());

            return builder;
        }
    }
}
