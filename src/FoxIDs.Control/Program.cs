using Azure.Identity;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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

                    var settings = context.Configuration.BindConfig<Settings>(nameof(Settings), validate: false);
                    var appInsightsSettings = context.Configuration.BindConfig<ApplicationInsights>(nameof(ApplicationInsights), validate: false);
                    if (settings.Options.Log == LogOptions.ApplicationInsights && !string.IsNullOrWhiteSpace(appInsightsSettings.ConnectionString))
                    {
                        logging.AddApplicationInsights(configuration => configuration.ConnectionString = appInsightsSettings.ConnectionString, options => { });
                        return;
                    }
                });
    }
}