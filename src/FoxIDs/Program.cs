using FoxIDs.Infrastructure.KeyVault;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

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
                .UseApplicationInsights()
                .ConfigureKestrel(options => options.AddServerHeader = false)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var builtConfig = builder.Build();
                    if (context.HostingEnvironment.IsProduction())
                    {
                        var keyVaultClient = FoxIDsKeyVaultClient.GetManagedClient();
                        builder.AddAzureKeyVault(builtConfig["Settings:KeyVault:EndpointUri"], keyVaultClient, new DefaultKeyVaultSecretManager());
                    }
                })
                .UseStartup<Startup>();
    }
}
