using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Hosting
{
    public class CorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public CorsPolicyProvider(FoxIDsSettings settings, TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository)
        {
            this.settings = settings;
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            if(!origin.IsNullOrEmpty())
            {
                var routeBinding = context.GetRouteBinding();
                if (routeBinding != null && routeBinding.DownParty != null && (routeBinding.DownParty.Type == PartyTypes.OAuth2 || routeBinding.DownParty.Type == PartyTypes.Oidc))
                {
                    var party = await tenantDataRepository.GetAsync<OAuthDownParty>(routeBinding.DownParty.Id);
                    if (party?.AllowCorsOrigins != null && party.AllowCorsOrigins.Count() > 0)
                    {
                        logger.ScopeTrace(() => $"Get CORS policy for origin '{origin}'.");
                        var corsPolicyBuilder = new CorsPolicyBuilder();

                        corsPolicyBuilder.WithOrigins(party.AllowCorsOrigins.ToArray())
                            .AllowAnyHeader()
                            .AllowAnyMethod();

                        corsPolicyBuilder.SetPreflightMaxAge(new TimeSpan(0, 0, settings.CorsPreflightMaxAge));

                        logger.ScopeTrace(() => "CORS policy added.");
                        return corsPolicyBuilder.Build();
                    }
                }
            }

            return null;
        }
    }
}
