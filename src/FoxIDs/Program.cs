using FoxIDs.Infrastructure.KeyVault;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var builtConfig = builder.Build();
                    if (!context.HostingEnvironment.IsDevelopment())
                    {
                        var keyVaultClient = FoxIDsKeyVaultClient.GetManagedClient();
                        builder.AddAzureKeyVault(builtConfig["Settings:KeyVault:EndpointUri"], keyVaultClient, new DefaultKeyVaultSecretManager());
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
