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

        protected override async Task<RouteValueDictionary> HandleRouteAsync(HttpContext httpContext, bool hasCustomDomain, RouteValueDictionary values, string[] route)
        {
            if (route.Length <= 3)
            {
                HandleWebSiteRoute(values, route);
            }
            else if ((!hasCustomDomain && route.Length >= 5 && route.Length <= 6) || (hasCustomDomain && route.Length >= 4 && route.Length <= 5))
            {
                await HandleTenantRouteAsync(httpContext, hasCustomDomain, values, route);
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

        private async Task HandleTenantRouteAsync(HttpContext httpContext, bool hasCustomDomain, RouteValueDictionary values, string[] route)
        {
            var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();

            scopedLogger.SetScopeProperty(Constants.Logs.Domain, httpContext.Request.Host.ToUriComponent());
            scopedLogger.SetScopeProperty(Constants.Logs.UserAgent, httpContext.Request.Headers["User-Agent"].ToString());

            var routeAction = route[route.Length - 1];
            if (routeAction.StartsWith('_'))
            {
                var routeController = (!hasCustomDomain && route.Length == 5) || (hasCustomDomain && route.Length == 4) ? route[route.Length - 2] : route[route.Length - 3];
                var subRoutAction = (!hasCustomDomain && route.Length == 5) || (hasCustomDomain && route.Length == 4) ? routeController : route[route.Length - 2];
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
            var sequence = await SetSequanceAsync(httpContext, routeAction);
            if (sequence != null && !sequence.Culture.IsNullOrEmpty())
            {
                scopedLogger.SetScopeProperty(Constants.Logs.SequenceCulture, sequence.Culture);
                return localizationLogic.GetSupportedCulture(new[] { sequence.Culture }, routeBinding);
            }
            else
            {
                var providerResultCulture = await new AcceptLanguageHeaderRequestCultureProvider().DetermineProviderCultureResult(httpContext);
                var culture = providerResultCulture?.UICultures?.Select(c => c.Value);
                if (!(culture?.Count() > 0))
                {
                    culture = providerResultCulture?.Cultures?.Select(c => c.Value);
                    if (!(culture?.Count() > 0))
                    {
                        culture = new[] { "en" };
                    }
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
