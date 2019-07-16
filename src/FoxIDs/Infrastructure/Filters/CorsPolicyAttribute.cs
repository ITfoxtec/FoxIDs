using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class CorsPolicyAttribute : TypeFilterAttribute
    {
        public CorsPolicyAttribute() : base(typeof(CorsPolicyActionAttribute))
        {
        }

        public class CorsPolicyActionAttribute : Attribute, ICorsPolicyProvider, IFilterMetadata
        {
            private readonly FoxIDsSettings settings;
            private readonly TelemetryScopedLogger logger;
            private readonly ITenantRepository tenantRepository;

            public CorsPolicyActionAttribute(FoxIDsSettings settings, TelemetryScopedLogger logger, ITenantRepository tenantRepository)
            {
                this.settings = settings;
                this.logger = logger;
                this.tenantRepository = tenantRepository;
            }

            public async Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
            {
                logger.ScopeTrace("Get Cors policy.");
                var corsPolicyBuilder = new CorsPolicyBuilder();

                var routeBinding = context.GetRouteBinding();
                if(routeBinding != null)
                {
                    logger.SetScopeProperty("downPartyId", routeBinding.DownParty.Id);

                    var party = await tenantRepository.GetAsync<OAuthDownParty>(routeBinding.DownParty.Id);
                    if(party?.AllowCorsOrigins != null && party.AllowCorsOrigins.Count() > 0)
                    {
                        corsPolicyBuilder.WithOrigins(party.AllowCorsOrigins.ToArray())
                            .AllowAnyMethod()
                            .AllowAnyHeader();

                        corsPolicyBuilder.SetPreflightMaxAge(new TimeSpan(0,0, settings.CorsPreflightMaxAge));

                        logger.ScopeTrace("Cors policy added.");
                    }
                }

                return corsPolicyBuilder.Build();
            }
        }
    }
}
