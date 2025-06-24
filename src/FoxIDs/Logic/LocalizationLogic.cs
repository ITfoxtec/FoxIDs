using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Logic
{
    public class LocalizationLogic : LogicSequenceBase
    {
        // The maximum number of culture names to attempt to test.
        private const int maximumCultureNamesToTry = 3;
        private readonly FoxIDsSettings settings;
        private readonly EmbeddedResourceLogic embeddedResourceLogic;

        public LocalizationLogic(FoxIDsSettings settings, IHttpContextAccessor httpContextAccessor, EmbeddedResourceLogic embeddedResourceLogic) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.embeddedResourceLogic = embeddedResourceLogic;
        }

        public string GetSupportedCulture(IEnumerable<string> cultures, RouteBinding routeBinding = null)
        {
            if (cultures?.Count() > 0)
            {
                var resourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();

                routeBinding = routeBinding ?? RouteBinding;
                var supportedCultures = resourceEnvelope.SupportedCultures;

                foreach (var culture in cultures.Take(maximumCultureNamesToTry))
                {
                    var supportedCulture = supportedCultures.Where(i => i.Equals(culture, StringComparison.InvariantCultureIgnoreCase) || i.Equals(new CultureInfo(culture).TwoLetterISOLanguageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (!supportedCulture.IsNullOrEmpty())
                    {
                        return culture;
                    }
                }
            }
            return Constants.Models.Resource.DefaultLanguage;
        }

        public string GetValue(string name, string culture)
        {
            var resourceEnvelope = embeddedResourceLogic.GetResourceEnvelope();

            var id = resourceEnvelope.Names.Where(n => n.Name == name).Select(n => n.Id).FirstOrDefault();
            if (id > 0)
            {
                var value = GetValue(resourceEnvelope, id, culture);
                if (!value.IsNullOrEmpty())
                {
                    return AddResourceId(id, value);
                }

                return AddResourceId(id, GetValue(resourceEnvelope, id, Constants.Models.Resource.DefaultLanguage));
            }

            (var trackValue, var trackId) = GetTrackResourceEnvelopeValue(name, culture);
            if (!trackValue.IsNullOrEmpty())
            {
                return AddResourceId(trackId, trackValue, isTrackId: true);
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
                    return value;
                }
            }

            return GetValue(resourceEnvelope.Resources, id, culture);
        }

        private (string, int) GetTrackResourceEnvelopeValue(string name, string culture)
        {
            if (RouteBinding?.ResourceEnvelope?.Names?.Count > 0)
            {
                var trackId = RouteBinding.ResourceEnvelope.Names.Where(n => n.Name == name).Select(n => n.Id).FirstOrDefault();
                if (trackId > 0)
                {
                    if (RouteBinding.ResourceEnvelope.Resources.Count > 0)
                    {
                        var value = GetValue(RouteBinding.ResourceEnvelope.Resources, trackId, culture);
                        if (!value.IsNullOrEmpty())
                        {
                            return (value, trackId);
                        }

                        value = GetValue(RouteBinding.ResourceEnvelope.Resources, trackId, Constants.Models.Resource.DefaultLanguage);
                        if (!value.IsNullOrEmpty())
                        {
                            return (value, trackId);
                        }
                    }

                    return (name, trackId);
                }
            }

            return (null, 0);
        }

        private string AddResourceId(int id, string value, bool isTrackId = false)
        {
            if (RouteBinding?.ShowResourceId == true)
            {
                return $"[{(isTrackId ? "T" : string.Empty)}{id}]{value}";
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
                return resource.Items.Where(i => i.Culture.Equals(culture, StringComparison.InvariantCultureIgnoreCase) || i.Culture.Equals(new CultureInfo(culture).TwoLetterISOLanguageName, StringComparison.InvariantCultureIgnoreCase)).Select(i => i.Value).FirstOrDefault();
            }
            return null;
        }

        public void SaveResource(string name)
        {
#if DEBUG
            if (settings.SaveNewResourceAsEmbeddedResource)
            {
                embeddedResourceLogic.SaveResource(name);
                return;
            }
#endif

            try
            {
                if (RouteBinding != null)
                {
                    var tenantDataRepository = HttpContext.RequestServices.GetService<ITenantDataRepository>();
                    var trackIdKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                    var mTrack = tenantDataRepository.GetTrackByNameAsync(trackIdKey).GetAwaiter().GetResult();

                    if (mTrack.ResourceEnvelope?.Names?.Any(n => n.Name == name) == true)
                    {
                        return;
                    }

                    if (mTrack.ResourceEnvelope == null)
                    {
                        mTrack.ResourceEnvelope = new TrackResourceEnvelope();
                    }
                    if (mTrack.ResourceEnvelope.Names == null)
                    {
                        mTrack.ResourceEnvelope.Names = new List<ResourceName>();
                    }
                    if (mTrack.ResourceEnvelope.Resources == null)
                    {
                        mTrack.ResourceEnvelope.Resources = new List<ResourceItem>();
                    }

                    var currentNumbers = mTrack.ResourceEnvelope.Names.Select(r => r.Id);
                    if (currentNumbers.Count() <= 0)
                    {
                        currentNumbers = [0];
                    }
                    var nextNumber = Enumerable.Range(1, currentNumbers.Max())
                             .Except(currentNumbers)
                             .DefaultIfEmpty(currentNumbers.Max() + 1)
                             .Min();

                    var resourceName = new ResourceName { Id = nextNumber, Name = name };
                    mTrack.ResourceEnvelope.Names.Add(resourceName);
                    // Do not create default ResourceItem because the text is not necessary English.

                    tenantDataRepository.UpdateAsync(mTrack).GetAwaiter().GetResult();
                    var trackCacheLogic = HttpContext.RequestServices.GetService<TrackCacheLogic>();
                    trackCacheLogic.InvalidateTrackCacheAsync(trackIdKey).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
                logger.Error(ex, "Add new resource name to environment error.");
            }
        }
    }
}
