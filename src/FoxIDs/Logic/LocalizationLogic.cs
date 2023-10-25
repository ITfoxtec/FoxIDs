using System;
using System.Collections.Generic;
using System.Linq;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class LocalizationLogic : LogicSequenceBase
    {
        // The maximum number of culture names to attempt to test.
        private const int maximumCultureNamesToTry = 3;
        private readonly EmbeddedResourceLogic embeddedResourceLogic;

        public LocalizationLogic(IHttpContextAccessor httpContextAccessor, EmbeddedResourceLogic embeddedResourceLogic) : base(httpContextAccessor)
        {
            this.embeddedResourceLogic = embeddedResourceLogic;
        }

        public string GetSupportedCulture(IEnumerable<string> cultures, RouteBinding routeBinding = null)
        {
            var resourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();

            routeBinding = routeBinding ?? RouteBinding;
            var supportedCultures = resourceEnvelope.SupportedCultures;
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
            return resourceEnvelope.SupportedCultures.First();
        }

        public string GetValue(string name, string culture)
        {
            var resourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();

            var id = resourceEnvelope.Names.Where(n => n.Name == name).Select(n => n.Id).FirstOrDefault();
            if(id > 0)
            {
                var value = GetValue(resourceEnvelope, id, culture);
                if (!value.IsNullOrEmpty())
                {
                    return value;
                }

                return GetValue(resourceEnvelope, id, "en");
            }

            return null;
        }

        private string GetValue(ResourceEnvelope resourceEnvelope, int id, string culture)
        {
            if (RouteBinding?.Resources?.Count > 0)
            {
                var value = GetValue(RouteBinding.Resources, id, culture);
                if (!value.IsNullOrEmpty())
                {
                    return AddResourceId(id, value);
                }
            }

            return AddResourceId(id, GetValue(resourceEnvelope.Resources, id, culture));
        }

        private string AddResourceId(int id, string value)
        {
            if (RouteBinding?.ShowResourceId == true)
            {
                return $"[{id}]{value}";
            }
            else
            {
                return value;
            }
        }

        private string GetValue(List<ResourceItem> resources, int id, string culture)
        {
            var resource = resources.Where(r => r.Id == id).FirstOrDefault();
            if (resource != null)
            {
                return resource.Items.Where(i => i.Culture.Equals(culture, StringComparison.InvariantCultureIgnoreCase) || i.Culture.StartsWith($"{culture}_", StringComparison.InvariantCultureIgnoreCase)).Select(i => i.Value).FirstOrDefault();
            }
            return null;
        }

#if DEBUG
        public void SaveResource(string name)
        {
            embeddedResourceLogic.SaveResource(name);
        }
#endif
    }
}
