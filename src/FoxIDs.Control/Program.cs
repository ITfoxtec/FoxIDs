using Azure.Identity;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using FoxIDs.Logic.Seed;
using System.Threading.Tasks;

namespace FoxIDs
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            var seed = scope.ServiceProvider.GetService<SeedLogic>();
            if (seed != null)
            {
                await seed.SeedAsync();
            }

            await host.RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel(options => options.AddServerHeader = false)
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (!context.HostingEnvironment.IsDevelopment())
                    {
                        var settings = config.Build().BindConfig<Settings>(nameof(Settings), validate: false);
                        if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault)
                        {
                            config.AddAzureKeyVault(new Uri(settings.KeyVault.EndpointUri), new DefaultAzureCredential());
                        }
                    }
                })
                .UseStartup<Startup>()
                .ConfigureLogging((context, logging) =>
                {
                    // Remove all loggers like console, debug, event source etc.
                    logging.ClearProviders();
                });
    }
}