using FoxIDs.Models.Api;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoxIDs.Infrastructure.ApiDescription
{
    public class NullableEnumSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema model, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                if (context.Type == typeof(ExternalConnectTypes) ||
                    context.Type == typeof(ClaimTransformTasks) ||
                    context.Type == typeof(PartyTypes))
                {
                    if (model is OpenApiSchema schema)
                    {
                        schema.Type = schema.Type.HasValue ? schema.Type | JsonSchemaType.Null : JsonSchemaType.Null;
                    }
                }
            }
        }
    }
}
