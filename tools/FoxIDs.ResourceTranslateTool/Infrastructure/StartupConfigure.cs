using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.ResourceTranslateTool.Models;
using FoxIDs.ResourceTranslateTool.Logic;
using DeepL;

namespace FoxIDs.ResourceTranslateTool.Infrastructure
{
    public class StartupConfigure
    {
        private ServiceCollection services;

        public IServiceProvider ConfigureServices()
        {
            services = new ServiceCollection();

            AddConfiguration();
            AddInfrastructure(services);
            AddLogic(services);

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        private static void AddLogic(ServiceCollection services)
        { 
            services.AddSingleton<ResourceLogic>();

            services.AddTransient<GoogleTranslateLogic>();
            services.AddTransient<DeepLTranslateLogic>();
        }

        private static void AddInfrastructure(ServiceCollection services)
        {
            services.AddSingleton(serviceProvider =>
            {
                var translateSettings = serviceProvider.GetService<TranslateSettings>();
                return new Translator(translateSettings.DeeplAuthenticationKey, new TranslatorOptions { ServerUrl = translateSettings.DeeplServerUrl });
            });
        }

        private void AddConfiguration()
        {
            var builder = new ConfigurationBuilder()
                                 .SetBasePath(Directory.GetCurrentDirectory())
                                 .AddJsonFile("appsettings.json")
                                 .AddJsonFile("appsettings.Development.json", optional: true);

            var configuration = builder.Build();
            services.BindConfig<TranslateSettings>(configuration, nameof(TranslateSettings));
        }
    }
}
