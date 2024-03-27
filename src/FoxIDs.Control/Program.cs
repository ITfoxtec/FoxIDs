using Azure.Identity;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace FoxIDs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel(options => options.AddServerHeader = false)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config.Build();
                    if (!context.HostingEnvironment.IsDevelopment())
                    {
                        var KeyStorageOption = builtConfig["Settings:Options:KeyStorage"];
                        if (string.IsNullOrWhiteSpace(KeyStorageOption) || KeyStorageOption.Equals(KeyStorageOptions.KeyVault.ToString(), StringComparison.Ordinal))
                        {
                            config.AddAzureKeyVault(new Uri(builtConfig["Settings:KeyVault:EndpointUri"]), new DefaultAzureCredential());
                        }
                    }
                })
                .UseStartup<Startup>()
                .ConfigureLogging((context, logging) =>
                {
                    var connectionString = context.Configuration.GetSection("ApplicationInsights:ConnectionString")?.Value;
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        return;
                    }

                    // When not in development, remove other loggers like console, debug, event source etc. and only use ApplicationInsights logging
                    if (!context.HostingEnvironment.IsDevelopment())
                    {
                        logging.ClearProviders();
                    }

                    logging.AddApplicationInsights(configuration => configuration.ConnectionString = connectionString, options => { });
                });
    }
}