using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Logic
{
    public class LargeLocalizationLogic : LogicSequenceBase
    {
        public LargeLocalizationLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public async Task<string> GetStringAsync(string resourceName)
        {
            var tenantDataRepository = GetTenantDataRepository();

            var normalizedName = resourceName.ToLowerInvariant();

            try
            {
                var resource = await GetOrCreateLargeResourceAsync(tenantDataRepository, normalizedName);
                return ResolveLargeResourceValue(resource);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load large resource '{normalizedName}'.", ex);
            }
        }

        private async Task<TrackLargeResource> GetOrCreateLargeResourceAsync(ITenantDataRepository repository, string resourceName)
        {
            var id = await TrackLargeResource.IdFormatAsync(RouteBinding, resourceName);

            var resource = await repository.GetAsync<TrackLargeResource>(id, required: false);
            if (resource != null)
            {
                return resource;
            }

            resource = new TrackLargeResource
            {
                Id = id,
                Name = resourceName
            };
            EnsureDefaultEnglishTranslation(resource);

            await repository.SaveAsync(resource);
            return resource;
        }

        private string ResolveLargeResourceValue(TrackLargeResource resource)
        {
            var cultureName = HttpContext.GetCultureParentName();

            var value = GetResourceCultureValue(resource.Items, cultureName);
            if (!value.IsNullOrWhiteSpace())
            {
                return value;
            }

            value = GetResourceCultureValue(resource.Items, Constants.Models.Resource.DefaultLanguage);
            if (value.IsNullOrWhiteSpace())
            {
                throw new Exception($"Default language '{Constants.Models.Resource.DefaultLanguage}' value is empty for large resource '{resource.Name}'.");
            }

            return value;
        }

        private static string GetResourceCultureValue(IEnumerable<TrackLargeResourceCultureItem> items, string culture)
        {
            if (culture.IsNullOrWhiteSpace())
            {
                return null;
            }

            var exactMatch = items.FirstOrDefault(i => i.Culture.Equals(culture, StringComparison.InvariantCultureIgnoreCase));
            if (exactMatch != null && !exactMatch.Value.IsNullOrWhiteSpace())
            {
                return exactMatch.Value;
            }

            return null;
        }

        private static void EnsureDefaultEnglishTranslation(TrackLargeResource resource)
        {
            resource.Items = [new TrackLargeResourceCultureItem
            {
                Culture = Constants.Models.Resource.DefaultLanguage,
                Value = $"Add the default English text for '{resource.Name}'."
            }];
        }

        private ITenantDataRepository GetTenantDataRepository()
        {
            var tenantDataRepository = HttpContext?.RequestServices?.GetService<ITenantDataRepository>();
            if (tenantDataRepository == null) 
            {
                throw new Exception($"{nameof(ITenantDataRepository)} not available.");
            }
            return tenantDataRepository;
        }
    }
}
