using Azure.Identity;
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
                        config.AddAzureKeyVault(new Uri(builtConfig["Settings:KeyVault:EndpointUri"]), new DefaultAzureCredential());
                    }
                })
                .UseStartup<Startup>()
                .ConfigureLogging((context, logging) =>
                {
                    var instrumentationKey = context.Configuration.GetSection("ApplicationInsights:InstrumentationKey").Value;

                    if (string.IsNullOrWhiteSpace(instrumentationKey))
                    {
                        return;
                    }

                    // When not in development, remove other loggers like console, debug, event source etc. and only use ApplicationInsights logging
                    if (!context.HostingEnvironment.IsDevelopment())
                    {
                        logging.ClearProviders();
                    }

                    logging.AddApplicationInsights(instrumentationKey);
                });
    }
}
