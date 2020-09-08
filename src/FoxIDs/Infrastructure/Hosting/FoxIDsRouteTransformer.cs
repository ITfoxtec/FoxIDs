using FoxIDs.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;
using UrlCombineLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Localization;
using FoxIDs.Models.Sequences;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsRouteTransformer : SiteRouteTransformer
    {
        private readonly LocalizationLogic localizationLogic;
        private readonly SequenceLogic sequenceLogic;

        public FoxIDsRouteTransformer(LocalizationLogic localizationLogic, SequenceLogic sequenceLogic)
        {
            this.localizationLogic = localizationLogic;
            this.sequenceLogic = sequenceLogic;
        }

        protected override string MapPath(string path)
        {
            if (path.EndsWith(IdentityConstants.OidcDiscovery.Path, StringComparison.InvariantCultureIgnoreCase))
            {
                return path.Replace(IdentityConstants.OidcDiscovery.Path, UrlCombine.Combine(Constants.Routes.OidcDiscoveryController, Constants.Routes.OidcDiscoveryAction), StringComparison.InvariantCultureIgnoreCase);
            }
            else if (path.EndsWith(IdentityConstants.OidcDiscovery.Keys, StringComparison.InvariantCultureIgnoreCase))
            {
                return path.Replace(UrlCombine.Combine(IdentityConstants.OidcDiscovery.Path, IdentityConstants.OidcDiscovery.Keys), UrlCombine.Combine(Constants.Routes.OidcDiscoveryController, Constants.Routes.OidcDiscoveryKeyAction), StringComparison.InvariantCultureIgnoreCase);
            }

            return path;
        }

        protected override async Task<RouteValueDictionary> HandleRouteAsync(HttpContext httpContext, RouteValueDictionary values, string[] route)
        {
            if (route.Length <= 3)
            {
                HandleWebSiteRoute(values, route);
            }
            else if (route.Length >= 5 && route.Length <= 6)
            {
                await HandleTenantRouteAsync(httpContext, values, route);
            }
            else
            {
                throw new NotSupportedException($"Route '{string.Join('/', route)}' not supported.");
            }

            return values;
        }

        private void HandleWebSiteRoute(RouteValueDictionary values, string[] route)
        {
            if(route.Length == 0)
            {
                values[Constants.Routes.RouteControllerKey] = Constants.Routes.DefaultSiteController;
                values[Constants.Routes.RouteActionKey] = Constants.Routes.DefaultSiteAction;
            }
            else if (route.Length >= 1)
            {
                values[Constants.Routes.RouteControllerKey] = route[0];
                if (route.Length >= 2)
                {
                    values[Constants.Routes.RouteActionKey] = route[1];
                }
                else
                {
                    values[Constants.Routes.RouteActionKey] = Constants.Routes.DefaultSiteAction;
                }
            }
        }

        private async Task HandleTenantRouteAsync(HttpContext httpContext, RouteValueDictionary values, string[] route)
        {
            var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();

            scopedLogger.SetScopeProperty("domain", httpContext.Request.Host.ToUriComponent());
            scopedLogger.SetScopeProperty("userAgent", httpContext.Request.Headers["User-Agent"].ToString());

            var routeAction = route[route.Length - 1];
            if (routeAction.StartsWith('_'))
            {
                var routeController = route.Length == 5 ? route[route.Length - 2] : route[route.Length - 3];
                var subRoutAction = route.Length == 5 ? routeController : route[route.Length - 2];
                values[Constants.Routes.RouteControllerKey] = routeController;
                values[Constants.Routes.RouteActionKey] = subRoutAction;

                await SetSequanceAndCulture(httpContext, scopedLogger, routeAction);
            }
            else
            {
                var routeController = route[route.Length - 2];
                values[Constants.Routes.RouteControllerKey] = routeController;
                values[Constants.Routes.RouteActionKey] = routeAction;
            }
        }

        protected async Task SetSequanceAndCulture(HttpContext httpContext, TelemetryScopedLogger scopedLogger, string routeAction)
        {
            var culture = await SetSequanceAndGetSupportedCultureAsync(httpContext, scopedLogger, routeAction);
            var requestCulture = new RequestCulture(culture);
            httpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, null));
        }

        private async Task<string> SetSequanceAndGetSupportedCultureAsync(HttpContext httpContext, TelemetryScopedLogger scopedLogger, string routeAction)
        {
            var routeBinding = httpContext.GetRouteBinding();
            var sequence = await SetSequanceAsync(httpContext, scopedLogger, routeAction);
            if (sequence != null && !sequence.Culture.IsNullOrEmpty())
            {
                return await localizationLogic.GetSupportedCultureAsync(new[] { sequence.Culture }, routeBinding);
            }
            else
            {
                var providerResultCulture = await new AcceptLanguageHeaderRequestCultureProvider().DetermineProviderCultureResult(httpContext);
                return await localizationLogic.GetSupportedCultureAsync(providerResultCulture.UICultures.Select(c => c.Value), routeBinding);
            }
        }

        private async Task<Sequence> SetSequanceAsync(HttpContext httpContext, TelemetryScopedLogger scopedLogger, string routeAction)
        {
            var sequenceString = routeAction.Substring(1);
            var sequence = await sequenceLogic.TryReadSequenceAsync(sequenceString);
            httpContext.Items[Constants.Routes.SequenceStringKey] = sequenceString;

            return sequence;
        }
    }
}
