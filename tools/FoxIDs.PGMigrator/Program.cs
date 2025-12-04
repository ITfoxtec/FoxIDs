using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using FoxIDs.Models.Config;
using FoxIDs.Infrastructure;
using FoxIDs;
using FoxIDs.PGMigrator;
using Wololo.PgKeyValueDB;

static void AddPgKeyValueDBSettings(PgKeyValueDBBuilder builder, Settings settings)
{
    builder.SchemaName = settings.PostgreSql.SchemaName;
    builder.TableName = settings.PostgreSql.TableName;
}
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpContextAccessor();
var settings = builder.Services.BindConfig<Settings>(builder.Configuration, nameof(Settings));
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<StdoutTelemetryLogger>();
builder.Services.AddSingleton<TelemetryLogger>();
builder.Services.AddSingleton<TelemetryScopedStreamLogger>();
builder.Services.AddScoped<TelemetryScopedLogger>();
builder.Services.AddScoped<TelemetryScopedProperties>();
builder.Services.AddScoped<ICosmosDbDataRepositoryClient, CosmosDbDataRepositoryClient>();
builder.Services.AddScoped<ICosmosDbDataRepositoryBulkClient, CosmosDbDataRepositoryBulkClient>();
builder.Services.AddScoped<CosmosDbTenantDataRepository>();
builder.Services.AddPgKeyValueDB(settings.PostgreSql.ConnectionString, a => AddPgKeyValueDBSettings(a, settings), ServiceLifetime.Singleton, Constants.Models.DataType.Tenant);
builder.Services.AddSingleton<ITenantDataRepository, PgTenantDataRepository>();
builder.Services.AddScoped<Migrator>();
var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var httpContext = new DefaultHttpContext
    {
        RequestServices = serviceProvider
    };
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    httpContextAccessor.HttpContext = httpContext;
    var app = serviceProvider.GetRequiredService<Migrator>();
    await app.RunAsync();
}