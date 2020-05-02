using FoxIDs.Infrastructure.ApiDescription;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class MvcCoreBuilderExtensions
    {
        public static IMvcBuilder AddFoxIDsApiExplorer(this IMvcBuilder builder)
        {
            builder.Services.AddSingleton<IApiDescriptionGroupCollectionProvider, FoxIDsApiDescriptionGroupCollectionProvider>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());

            return builder;
        }
    }
}
