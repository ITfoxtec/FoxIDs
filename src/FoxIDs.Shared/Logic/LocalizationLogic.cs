using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class LocalizationLogic : LogicBase
    {
        // The maximum number of culture names to attempt to test.
        private const int maximumCultureNamesToTry = 3;
        private readonly IMasterRepository masterRepository;
        private ResourceEnvelope resourceEnvelope;
        private bool isInitiated = false;

        public LocalizationLogic(IHttpContextAccessor httpContextAccessor, IMasterRepository masterRepository) : base(httpContextAccessor)
        {
            this.masterRepository = masterRepository;
        }

        private async Task LoadResourceEnvelopeAsync()
        {
            if(!isInitiated)
            {
                resourceEnvelope = await masterRepository.GetAsync<ResourceEnvelope>(ResourceEnvelope.IdFormat(new MasterDocument.IdKey()));
                isInitiated = true;
            }
        }

        public async Task<string> GetSupportedCultureAsync(IEnumerable<string> cultures, RouteBinding routeBinding = null)
        {
            await LoadResourceEnvelopeAsync();

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

        public async Task<string> GetValueAsync(string name, string culture)
        {
            await LoadResourceEnvelopeAsync();

            var id = resourceEnvelope.Names.Where(n => n.Name == name).Select(n => n.Id).FirstOrDefault();
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
                    var value = GetValue(resourceEnvelope.Resources, id, culture);
                    if (!value.IsNullOrEmpty())
                    {
                        return value;
                    }
                }

                return GetValue(resourceEnvelope.Resources, id, "en");
            }

            return null;
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
        public void SaveDefaultValue(string name)
        {
            LoadResourceEnvelopeAsync().GetAwaiter().GetResult();

            lock (typeof(LocalizationLogic))
            {
                if (!resourceEnvelope.Names.Any(n => n.Name == name))
                {
                    var id = resourceEnvelope.Names.Max(n => n.Id) + 1;
                    resourceEnvelope.Names.Add(new ResourceName { Name = name, Id = id });
                    resourceEnvelope.Resources.Add(new ResourceItem { Id = id, Items = new List<ResourceCultureItem>(new[] { new ResourceCultureItem { Culture = "en", Value = name } }) });

                    resourceEnvelope.ValidateObjectAsync().GetAwaiter().GetResult();

                    masterRepository.SaveAsync(resourceEnvelope).GetAwaiter().GetResult();
                }
            }
        }
#endif
    }
}
