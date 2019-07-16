using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FoxIDs.Models;
using FoxIDs.Models.Resources;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Logic
{
    public class LocalizationLogic : LogicBase
    {
        // The maximum number of culture names to attempt to test.
        private const int maximumCultureNamesToTry = 3;

        private EmbeddedResource embeddedResource;

        public LocalizationLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            Load();
        }

        private void Load()
        {
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(EmbeddedResource).FullName}.json")))
            {
                embeddedResource = reader.ReadToEnd().ToObject<EmbeddedResource>();
                embeddedResource.ValidateObjectAsync().GetAwaiter().GetResult();
            }
        }

        public string GetSupportedCulture(IEnumerable<string> cultures, RouteBinding routeBinding = null)
        {
            routeBinding = routeBinding ?? RouteBinding;
            var supportedCultures = embeddedResource.SupportedCultures;
            if(routeBinding.Resources?.Count > 0)
            {
                var trackSupportedCultures = routeBinding.Resources.SelectMany(r => r.Items.GroupBy(ig => ig.Culture)).Select(rk => rk.Key);
                supportedCultures = supportedCultures.ConcatOnce(trackSupportedCultures);
            }

            foreach (var culture in cultures.Take(maximumCultureNamesToTry))
            {
                if (supportedCultures.Where(c => c.Equals(culture, StringComparison.InvariantCultureIgnoreCase) || c.StartsWith($"{culture}_", StringComparison.InvariantCultureIgnoreCase)).Any())
                {
                    return culture;
                }
            }
            return embeddedResource.SupportedCultures.First();
        }

        public string GetValue(string name, string culture)
        {
            var id = embeddedResource.Names.Where(n => n.Name == name).Select(n => n.Id).FirstOrDefault();
            if(id > 0)
            {
                if (RouteBinding.Resources?.Count > 0)
                {
                    var value = GetValue(RouteBinding.Resources, id, culture);
                    if (!value.IsNullOrEmpty())
                    {
                        return value;
                    }
                }
                else
                {
                    var value = GetValue(embeddedResource.Resources, id, culture);
                    if (!value.IsNullOrEmpty())
                    {
                        return value;
                    }
                }

                return GetValue(embeddedResource.Resources, id, "en");
            }

            return null;
        }

        private string GetValue(List<Resource> resources, int id, string culture)
        {
            var resource = resources.Where(r => r.Id == id).FirstOrDefault();
            if (resource != null)
            {
                return resource.Items.Where(i => i.Culture.Equals(culture, StringComparison.InvariantCultureIgnoreCase) || i.Culture.StartsWith($"{culture}_", StringComparison.InvariantCultureIgnoreCase)).Select(i => i.Value).FirstOrDefault();
            }
            return null;
        }

#if DEBUG
        public void SaveDefaultValue(string name)
        {
            if (!embeddedResource.Names.Any(n => n.Name == name))
            {
                var id = embeddedResource.Names.Max(n => n.Id) + 1;
                embeddedResource.Names.Add(new ResourceName { Name = name, Id = id });
                embeddedResource.Resources.Add(new Resource { Id = id, Items = new List<ResourceItem>(new[] { new ResourceItem { Culture = "en", Value = name } }) });

                embeddedResource.ValidateObjectAsync().GetAwaiter().GetResult();

                var foxIDsLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var embeddedResourceFilePath = $"{foxIDsLocation}\\..\\..\\..\\..\\FoxIDs.Shared\\Models\\Resources\\{nameof(EmbeddedResource)}.json";
                File.WriteAllText(embeddedResourceFilePath, embeddedResource.ToJsonIndented());
            }
        }
#endif
    }
}
