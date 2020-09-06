using FoxIDs.Infrastructure;
using FoxIDs.Models.Master.SeedResources;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class ResourceSeedLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly IMasterRepository masterRepository;

        public ResourceSeedLogic(TelemetryLogger logger, IMasterRepository masterRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.masterRepository = masterRepository;
        }

        public async Task SeedAsync()
        {
            try
            {
                var id = ResourceEnvelope.IdFormat(new MasterDocument.IdKey());
                try
                {
                    _ = await masterRepository.GetAsync<ResourceEnvelope>(id);
                }
                catch (CosmosDataException ex)
                {
                    if (ex.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw new Exception($"{id} document exists.");
                    }
                }

                var resourceEnvelope = LoadResource();
                resourceEnvelope.Id = id;

                await masterRepository.SaveAsync(resourceEnvelope);
            }
            catch (Exception ex)
            {
                logger.CriticalError(ex, "Error seeding master resource document.");
                throw;
            }
        }

        private ResourceEnvelope LoadResource()
        {
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(EmbeddedResource).FullName}.json")))
            {
                return reader.ReadToEnd().ToObject<ResourceEnvelope>();
            }
        }
    }
}
