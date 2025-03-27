using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace FoxIDs
{
    public static class HttpContextExtensions
    {
        public static string GetHostWithTenantAndTrack(this HttpContext context, string trackName = null)
        {
            var routeBinding = context.GetRouteBinding();
            if (!routeBinding.UseCustomDomain)
            {
                return UrlCombine.Combine(context.GetHost(), routeBinding.TenantName, trackName ?? routeBinding.TrackName);
            }
            else
            {
                return UrlCombine.Combine(context.GetHost(), trackName ?? routeBinding.TrackName);
            }
        }

        public static string GetHostWithRoute(this HttpContext context, string routeUrl)
        {
            var routeBinding = context.GetRouteBinding();
            if (!routeBinding.UseCustomDomain)
            {
                return UrlCombine.Combine(context.GetHost(), routeUrl);
            }
            else
            {
                return UrlCombine.Combine(context.GetHost(), routeUrl);
            }
        }

        public static string GetHostWithRouteOrBinding(this HttpContext context, bool usePartyIssuer)
        {
            var routeBinding = context.GetRouteBinding();
            if (usePartyIssuer)
            {
                return context.GetHostWithRoute(routeBinding.RouteUrl);
            }
            else
            {
                return UrlCombine.Combine(context.GetHostWithTenantAndTrack(), routeBinding.PartyNameAndBinding);
            }
        }

        public static CultureInfo GetCulture(this HttpContext context)
        {
            return context.Features.Get<IRequestCultureFeature>()?.RequestCulture?.Culture ?? new CultureInfo(Constants.Models.Resource.DefaultLanguage);
        }

        public static CultureInfo GetUiCulture(this HttpContext context)
        {
            return context.Features.Get<IRequestCultureFeature>()?.RequestCulture?.UICulture;
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
