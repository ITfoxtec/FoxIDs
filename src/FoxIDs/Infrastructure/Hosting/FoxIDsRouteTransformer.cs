using FoxIDs.Logic;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;
using UrlCombineLib;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Localization;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsRouteTransformer : SiteRouteTransformer
    {
        private readonly IServiceProvider serviceProvider;
        private readonly LocalizationLogic localizationLogic;
        private readonly SequenceLogic sequenceLogic;

        public FoxIDsRouteTransformer(IServiceProvider serviceProvider, LocalizationLogic localizationLogic, SequenceLogic sequenceLogic, ITenantRepository tenantRepository) : base(tenantRepository)
        {
            this.serviceProvider = serviceProvider;
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
            if (route.Length >= 1)
            {
                values[Constants.Routes.RouteControllerKey] = route[1];
                if (route.Length >= 2)
                {
                    values[Constants.Routes.RouteActionKey] = route[2];
                }
            }
        }

        private async Task HandleTenantRouteAsync(HttpContext httpContext, RouteValueDictionary values, string[] route)
        {
            var trackIdKey = new Track.IdKey
            {
                TenantName = route[0].ToLower(),
                TrackName = route[1].ToLower()
            };
            var partyNameAndbinding = route[2].ToLower();

            var scopedLogger = serviceProvider.GetService<TelemetryScopedLogger>();
            try
            {
                var routeBinding = await GetRouteDataAsync(scopedLogger, trackIdKey, partyNameAndbinding);
                httpContext.Items[Constants.Routes.RouteBindingKey] = routeBinding;

                scopedLogger.SetScopeProperty(Constants.Routes.RouteBindingKey, new { routeBinding.TenantName, routeBinding.TrackName, routeBinding.PartyNameAndBinding }.ToJson());

                var routeAction = route[route.Length - 1];
                if (routeAction.StartsWith('_'))
                {
                    var routeController = route.Length == 5 ? route[route.Length - 2] : route[route.Length - 3];
                    var subRoutAction = route.Length == 5 ? routeController : route[route.Length - 2];
                    values[Constants.Routes.RouteControllerKey] = routeController;
                    values[Constants.Routes.RouteActionKey] = subRoutAction;

                    await SetCulture(httpContext, scopedLogger, routeBinding, routeAction);
                }
                else
                {
                    var routeController = route[route.Length - 2];
                    values[Constants.Routes.RouteControllerKey] = routeController;
                    values[Constants.Routes.RouteActionKey] = routeAction;
                }
            }
            catch (ValidationException vex)
            {
                scopedLogger.Error(vex);
                throw;
            }
        }

        protected async Task SetCulture(HttpContext httpContext, TelemetryScopedLogger scopedLogger, RouteBinding routeBinding, string routeAction)
        {
            var culture = await GetSupportedCultureAsync(httpContext, scopedLogger, routeBinding, routeAction);
            var requestCulture = new RequestCulture(culture);
            httpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, null));
        }

        private async Task<string> GetSupportedCultureAsync(HttpContext httpContext, TelemetryScopedLogger scopedLogger, RouteBinding routeBinding, string routeAction)
        {
            var sequence = await GetSequanceAsync(httpContext, scopedLogger, routeAction);
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

        private async Task<Sequence> GetSequanceAsync(HttpContext httpContext, TelemetryScopedLogger scopedLogger, string routeAction)
        {
            var sequenceString = routeAction.Substring(1);
            var sequence = await sequenceLogic.TryReadSequenceAsync(sequenceString);

            if (sequence != null)
            {
                scopedLogger.SetScopeProperty("sequenceId", sequence.Id);
            }
            httpContext.Items[Constants.Routes.SequenceStringKey] = sequenceString;

            return sequence;
        }
    }
}
