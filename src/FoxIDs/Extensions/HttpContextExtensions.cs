using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace FoxIDs
{
    public static class HttpContextExtensions
    {
        public static string GetHostWithTenantAndTrack(this HttpContext context, string trackName = null, bool useConfig = false)
        {
            var routeBinding = context.GetRouteBinding();
            if (!routeBinding.UseCustomDomain)
            {
                return UrlCombine.Combine(context.GetHost(useConfig: useConfig), routeBinding.TenantName, trackName ?? routeBinding.TrackName);;
            }
            else
            {
                return UrlCombine.Combine(context.GetHost(useConfig: useConfig), trackName ?? routeBinding.TrackName);
            }
        }

        public static CultureInfo GetCulture(this HttpContext context)
        {
            return context.Features.Get<IRequestCultureFeature>()?.RequestCulture?.UICulture ?? new CultureInfo(Constants.Models.Resource.DefaultLanguage);
        }

        public static string GetCultureParentName(this HttpContext context)
        {
            var culture = context.GetCulture();
            if (culture.Parent != null && !culture.Parent.Name.IsNullOrWhiteSpace())
            {
                return culture.Parent.Name.ToLower();
            }
            else
            {
                return culture.Name.ToLower();
            }
        }
    }
}
