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
                        var keyStorageOption = builtConfig["Settings:Options:KeyStorage"];
                        if (string.IsNullOrWhiteSpace(keyStorageOption) || keyStorageOption.Equals(KeyStorageOptions.KeyVault.ToString(), StringComparison.Ordinal))
                        {
                            config.AddAzureKeyVault(new Uri(builtConfig["Settings:KeyVault:EndpointUri"]), new DefaultAzureCredential());
                        }
                    }
                })
                .UseStartup<Startup>()
                .ConfigureLogging((context, logging) =>
                {
                    // Remove all loggers like console, debug, event source etc.
                    logging.ClearProviders();

                    var logOption = context.Configuration["Settings:Options:Log"];
                    if (string.IsNullOrWhiteSpace(logOption) || logOption.Equals(LogOptions.ApplicationInsights.ToString(), StringComparison.Ordinal))
                    {
                        var connectionString = context.Configuration.GetSection("ApplicationInsights:ConnectionString")?.Value;
                        if (!string.IsNullOrWhiteSpace(connectionString))
                        {
                            logging.AddApplicationInsights(configuration => configuration.ConnectionString = connectionString, options => { });
                            return;
                        }
                    }
                });
    }
}