using FoxIDs.Models;
using FoxIDs.Models.Master.Resources;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FoxIDs.Logic
{
    public class EmbeddedResourceLogic : LogicBase
    {
        private ResourceEnvelope resourceEnvelope;
        private bool isInitiated = false;

        public EmbeddedResourceLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public ResourceEnvelope GetResourceEnvelope()
        {
            LoadResources();
            return resourceEnvelope;
        }

#if DEBUG
        public void SaveResource(string name)
        {
            LoadResources();

            lock (typeof(EmbeddedResourceLogic))
            {
                if (!resourceEnvelope.Names.Any(n => n.Name == name))
                {
                    var id = resourceEnvelope.Names.Max(n => n.Id) + 1;
                    resourceEnvelope.Names.Add(new ResourceName { Name = name, Id = id });
                    resourceEnvelope.Resources.Add(new ResourceItem { Id = id, Items = new List<ResourceCultureItem>(new[] { new ResourceCultureItem { Culture = "en", Value = name } }) });

                    resourceEnvelope.ValidateObjectAsync().GetAwaiter().GetResult();

                    File.WriteAllText(EmbeddedResourceFile, resourceEnvelope.ToJsonIndented());
                }
            }
        }
#endif

        private void LoadResources()
        {
            if (!isInitiated)
            {
                using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedResourceName)))
                {
                    resourceEnvelope = reader.ReadToEnd().ToObject<ResourceEnvelope>();
                }
                isInitiated = true;
            }
        }

        private string EmbeddedResourceName => $"{typeof(EmbeddedResource).FullName}.json";

        private string EmbeddedResourceFile => EmbeddedResourceName.Replace('.', '\\').Replace(@"\json", ".json").Replace(@"FoxIDs\", @"..\FoxIDs.Shared\");
    }
}
