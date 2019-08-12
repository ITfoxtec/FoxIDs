using FoxIDs.Model;
using FoxIDs.SeedTool.Logic;
using FoxIDs.SeedTool.Model;
using FoxIDs.SeedTool.Model.Resources;
using ITfoxtec.Identity;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.SeedTool.SeedLogic
{
    public class ResourceSeedLogic
    {
        private readonly SeedSettings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly AccessLogic accessLogic;

        public ResourceSeedLogic(SeedSettings settings, IHttpClientFactory httpClientFactory, AccessLogic accessLogic)
        {
            this.settings = settings;
            this.httpClientFactory = httpClientFactory;
            this.accessLogic = accessLogic;
        }

        public string ResourceApiEndpoint => UrlCombine.Combine(settings.FoxIDsMasterApiEndpoint, "Resource");

        public async Task SeedAsync()
        {
            Console.WriteLine("Create master resource document");

            var resourceApiModel = LoadResource();
            await SaveResourceDocumentAsync(resourceApiModel);

            Console.WriteLine(string.Empty);
            Console.WriteLine($"Master resource document created and saved in Cosmos DB");
        }

        private ResourceApiModel LoadResource()
        {
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(EmbeddedResource).FullName}.json")))
            {
                return reader.ReadToEnd().ToObject<ResourceApiModel>();
            }
        }

        private async Task SaveResourceDocumentAsync(ResourceApiModel resourceApiModel)
        {
            var accessToken = await accessLogic.GetAccessTokenAsync();

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using (var response = await client.PostJsonAsync(ResourceApiEndpoint, resourceApiModel))
            {
                await response.ValidateResponseAsync();
            }
        }

    }
}
