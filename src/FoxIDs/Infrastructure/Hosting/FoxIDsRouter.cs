using ITfoxtec.Identity;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using UrlCombineLib;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Localization;
using System.Linq;

namespace FoxIDs.Infrastructure.Hosting
{
    public class FoxIDsRouter : SiteRouter
    {
        public FoxIDsRouter(IRouter defaultRouter) : base(defaultRouter)
        { }

        protected override async Task HandleRouteAsync(RouteContext context)
        {
            var path = MapPathToController(context.HttpContext.Request.Path.Value);
            var pathSplit = path.Split('/');

            if (pathSplit.Length <= 3)
            {
                HandleWebSiteRoute(context, pathSplit);
            }
            else if (pathSplit.Length >= 6 && pathSplit.Length <= 7)
            {
                await HandleTenantRouteAsync(context, pathSplit);
            }
            else
            {
                throw new NotSupportedException($"Invalid route path. Url '{context.HttpContext.Request.Path.Value}' not supported.");
            }
        }

        private string MapPathToController(string path)
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

        private void HandleWebSiteRoute(RouteContext context, string[] path)
        {
            if (path.Length >= 2)
            {
                context.RouteData.Values[Constants.Routes.RouteControllerKey] = path[1];
                if (path.Length >= 3)
                {
                    context.RouteData.Values[Constants.Routes.RouteActionKey] = path[2];
                }
            }
        }

        private async Task HandleTenantRouteAsync(RouteContext context, string[] path)
        {
            var trackIdKey = new Track.IdKey
            {
                TenantName = path[1].ToLower(),
                TrackName = path[2].ToLower()
            };
            var partyNameAndbinding = path[3].ToLower();

            var scopedLogger = context.HttpContext.RequestServices.GetService<TelemetryScopedLogger>();
            try
            {
                var routeBinding = await GetRouteDataAsync(scopedLogger, context.HttpContext, trackIdKey, partyNameAndbinding);
                context.RouteData.DataTokens[Constants.Routes.RouteBindingKey] = routeBinding;

                scopedLogger.SetScopeProperty(Constants.Routes.RouteBindingKey, new { routeBinding.TenantName, routeBinding.TrackName, routeBinding.PartyNameAndBinding }.ToJson());

                var routeAction = path[path.Length - 1];
                if (routeAction.StartsWith('_'))
                {
                    var routeController = path.Length == 6 ? path[path.Length - 2] : path[path.Length - 3];
                    var subRoutAction = path.Length == 6 ? routeController : path[path.Length - 2];
                    context.RouteData.Values[Constants.Routes.RouteControllerKey] = routeController;
                    context.RouteData.Values[Constants.Routes.RouteActionKey] = subRoutAction;

                    await SetCulture(context, scopedLogger, routeBinding, routeAction);
                }
                else
                {
                    var routeController = path[path.Length - 2];
                    context.RouteData.Values[Constants.Routes.RouteControllerKey] = routeController;
                    context.RouteData.Values[Constants.Routes.RouteActionKey] = routeAction;
                }
            }
            catch (ValidationException vex)
            {
                scopedLogger.Error(vex);
                throw;
            }
        }

        protected async Task SetCulture(RouteContext context, TelemetryScopedLogger scopedLogger, RouteBinding routeBinding, string routeAction)
        {
            var culture = await GetSupportedCultureAsync(context, scopedLogger, routeBinding, routeAction);
            var requestCulture = new RequestCulture(culture);
            context.HttpContext.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, null));
        }

        private async Task<string> GetSupportedCultureAsync(RouteContext context, TelemetryScopedLogger scopedLogger, RouteBinding routeBinding, string routeAction)
        {
            var localizationLogic = context.HttpContext.RequestServices.GetService<LocalizationLogic>();
            var sequence = await GetSequanceAsync(context, scopedLogger, routeAction);
            if (sequence != null && !sequence.Culture.IsNullOrEmpty())
            {
                return await localizationLogic.GetSupportedCultureAsync(new[] { sequence.Culture }, routeBinding);
            }
            else
            {
                var providerResultCulture = await new AcceptLanguageHeaderRequestCultureProvider().DetermineProviderCultureResult(context.HttpContext);
                return await localizationLogic.GetSupportedCultureAsync(providerResultCulture.UICultures.Select(c => c.Value), routeBinding);
            }
        }

        private async Task<Sequence> GetSequanceAsync(RouteContext context, TelemetryScopedLogger scopedLogger, string routeAction)
        {
            var sequenceString = routeAction.Substring(1);
            var sequenceLogic = context.HttpContext.RequestServices.GetService<SequenceLogic>();
            var sequence = await sequenceLogic.TryReadSequenceAsync(sequenceString);

            if(sequence != null)
            {
                scopedLogger.SetScopeProperty("sequenceId", sequence.Id);
            }
            context.RouteData.DataTokens[Constants.Routes.SequenceStringKey] = sequenceString;

            return sequence;
        }
    }
}
