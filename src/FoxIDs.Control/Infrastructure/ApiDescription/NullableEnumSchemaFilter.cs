using FoxIDs.Models.Api;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoxIDs.Infrastructure.ApiDescription
{
    public class NullableEnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema model, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                if (context.Type == typeof(ExternalConnectTypes) ||
                    context.Type == typeof(ClaimTransformTasks) ||
                    context.Type == typeof(PartyTypes))
                {
                    model.Nullable = true; 
                }
            }
        }
    }
}
