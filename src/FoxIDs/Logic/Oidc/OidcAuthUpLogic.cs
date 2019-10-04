using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcAuthUpLogic<TParty, TClient, TScope, TClaim> : LogicBase where TParty : OidcDownParty where TClient : OidcDownClient<TScope, TClaim> where TScope : OidcDownScope<TClaim> where TClaim : OidcDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;

        public OidcAuthUpLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
        }

        public async Task<IActionResult> AuthenticationRequestAsync(UpPartyLink partyLink)
        {
            throw new NotImplementedException();
        }
    }
}
