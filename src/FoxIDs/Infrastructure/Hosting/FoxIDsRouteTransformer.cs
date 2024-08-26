using FoxIDs.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;
using ITfoxtec.Identity.Util;
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

        protected override bool CheckCustomDomainSupport(string[] route)
        {
            if (route.Length > 1)
            {
                if (route[0].Equals(Constants.Routes.MasterTrackName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (route.Length > 2)
                {
                    if (route[1].Equals(Constants.Routes.MasterTrackName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
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

        protected override async Task<RouteValueDictionary> HandleRouteAsync(HttpContext httpContext, bool useCustomDomain, RouteValueDictionary values, string[] route)
        {
            if (route.Length <= 3)
            {
                HandleWebSiteRoute(values, route);
            }
            else
            {
                route = CompoundSequanceString(route);
                if ((!useCustomDomain && route.Length >= 5 && route.Length <= 6) || (useCustomDomain && route.Length >= 4 && route.Length <= 5))
                {
                    await HandleTenantRouteAsync(httpContext, useCustomDomain, values, route);
                }
                else
                {
                    throw new NotSupportedException($"Route '{string.Join('/', route)}' not supported.");
                }
            }
            return values;
        }

        private string[] CompoundSequanceString(string[] route)
        {
            if (route[route.Length - 2].StartsWith('_'))
            {
                var tenantRoute = new string[route.Length - 1];
                for(var i = 0; i <= route.Length - 2; i++)
                {
                    if (i < route.Length - 2)
                    {
                        tenantRoute[i] = route[i];
                    }
                    else
                    {
                        tenantRoute[i] = $"{route[i]}/{route[i + 1]}";
                    }
                }
                return tenantRoute;
            }

            return route;
        }

        private void HandleWebSiteRoute(RouteValueDictionary values, string[] route)
        {
            if(route.Length == 0)
            {
                values[Constants.Routes.RouteControllerKey] = Constants.Routes.DefaultSiteController;
                values[Constants.Routes.RouteActionKey] = Constants.Routes.DefaultAction;
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
                    values[Constants.Routes.RouteActionKey] = Constants.Routes.DefaultAction;
                }
            }
        }

        private async Task HandleTenantRouteAsync(HttpContext httpContext, bool useCustomDomain, RouteValueDictionary values, string[] route)
        {
            var routeAction = route[route.Length - 1];
            if (routeAction.StartsWith('_'))
            {
                var routeController = (!useCustomDomain && route.Length == 5) || (useCustomDomain && route.Length == 4) ? route[route.Length - 2] : route[route.Length - 3];
                var subRoutAction = (!useCustomDomain && route.Length == 5) || (useCustomDomain && route.Length == 4) ? routeController : route[route.Length - 2];
                values[Constants.Routes.RouteControllerKey] = routeController;
                values[Constants.Routes.RouteActionKey] = subRoutAction;

                await SetSequanceAndCulture(httpContext, routeAction);
            }
            else
            {
                var routeController = route[route.Length - 2];
                values[Constants.Routes.RouteControllerKey] = routeController;
                values[Constants.Routes.RouteActionKey] = routeAction;
            }
        }

        protected async Task SetSequanceAndCulture(HttpContext httpContext, string routeAction)
        {
            var culture = await SetSequanceAndGetSupportedCultureAsync(httpContext, routeAction);
            var requestCulture = new RequestCulture(culture);
            httpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, null));
        }

        private async Task<string> SetSequanceAndGetSupportedCultureAsync(HttpContext httpContext, string routeAction)
        {
            var routeBinding = httpContext.GetRouteBinding();
            var sequence = await SetSequanceAsync(httpContext, routeAction);
            if (sequence != null && !sequence.Culture.IsNullOrEmpty())
            {
                var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();
                scopedLogger.SetScopeProperty(Constants.Logs.SequenceCulture, sequence.Culture);
                return localizationLogic.GetSupportedCulture([sequence.Culture], routeBinding);
            }
            else
            {
                var providerResultCulture = await new AcceptLanguageHeaderRequestCultureProvider().DetermineProviderCultureResult(httpContext);
                var culture = providerResultCulture?.UICultures?.Select(c => c.Value);
                if (!(culture?.Count() > 0))
                {
                    culture = providerResultCulture?.Cultures?.Select(c => c.Value);
                }
                return localizationLogic.GetSupportedCulture(culture, routeBinding);
            }
        }

        private async Task<Sequence> SetSequanceAsync(HttpContext httpContext, string routeAction)
        {
            var sequenceString = routeAction.Substring(1);
            var sequence = await sequenceLogic.TryReadSequenceAsync(sequenceString);
            httpContext.Items[Constants.Routes.SequenceStringKey] = sequenceString;

            return sequence;
        }
    }
}
