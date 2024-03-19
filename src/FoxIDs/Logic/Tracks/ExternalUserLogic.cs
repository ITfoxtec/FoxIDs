using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ExternalUserLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly ClaimTransformLogic claimTransformLogic;

        public ExternalUserLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, ClaimTransformLogic claimTransformLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.claimTransformLogic = claimTransformLogic;
        }

        public async Task<(bool disableAccount, string linkClaim, IEnumerable<Claim> externalUserClaims)> GetUserAsync(ExternalUserUpParty upParty, IEnumerable<Claim> claims)
        {
            var linkClaim = GetLinkClaim(upParty, claims);
            logger.ScopeTrace(() => $"Validating external user, link claim value '{linkClaim}', Route '{RouteBinding?.Route}'.");
            if (linkClaim.IsNullOrWhiteSpace())
            {
                return (false, linkClaim, null);
            }

            var externalUser = await tenantRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, upParty.Name, await linkClaim.HashIdStringAsync()), required: false);
            if (externalUser == null || externalUser.DisableAccount)
            {
                return (externalUser == null ? false : externalUser.DisableAccount, linkClaim, null);
            }

            var externalUserClaims = externalUser.Claims?.ToClaimList() ?? new List<Claim>();
            externalUserClaims = AddUserIdClaim(upParty, externalUserClaims, externalUser);
            logger.ScopeTrace(() => $"AuthMethod, External user output JWT claims '{externalUserClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return (externalUser == null ? false : externalUser.DisableAccount, linkClaim, externalUserClaims);
        }

        public async Task<IEnumerable<Claim>> CreateUserAsync(ExternalUserUpParty upParty, string linkClaim, IEnumerable<Claim> externalUserClaims = null)
        {
            logger.ScopeTrace(() => $"Creating external user, link claim value '{linkClaim}', Route '{RouteBinding?.Route}'.");

            externalUserClaims = externalUserClaims ?? new List<Claim>();
            var transformedClaims = await claimTransformLogic.Transform(upParty.LinkExternalUser.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), externalUserClaims);

            var externalUser = new ExternalUser
            {
                Id = await ExternalUser.IdFormatAsync(RouteBinding, upParty.Name, await linkClaim.HashIdStringAsync()),
                UserId = Guid.NewGuid().ToString(),
                LinkClaimValue = linkClaim,
                Claims = transformedClaims.ToClaimAndValues()
            };

            await tenantRepository.CreateAsync(externalUser);
            logger.ScopeTrace(() => $"External user created, with user id '{externalUser.UserId}'.");

            transformedClaims = AddUserIdClaim(upParty, transformedClaims, externalUser);
            logger.ScopeTrace(() => $"AuthMethod, Create external user output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return transformedClaims;
        }  
        
        public async Task<IActionResult> StartUICreateUserAsync(ExternalUserUpParty upParty, string linkClaim, IEnumerable<Claim> claims, string externalSessionId, Saml2StatusCodes saml2Status)
        {
            logger.ScopeTrace(() => $"Start UI create external user, link claim '{linkClaim}', Route '{RouteBinding?.Route}'.");
            await sequenceLogic.SaveSequenceDataAsync(new ExternalUserUpSequenceData
            {
                UpPartyId = upParty.Id,
                UpPartyType = upParty.Type,
                Claims = claims.ToClaimAndValues(),
                LinkClaimValue = linkClaim,
                ExternalSessionId = externalSessionId,
                Saml2Status = saml2Status
            });
            return HttpContext.GetUpPartyUrl(upParty.Name, Constants.Routes.ExtController, Constants.Endpoints.CreateUser, includeSequence: true).ToRedirectResult(RouteBinding.DisplayName);
        }

        private List<Claim> AddUserIdClaim(ExternalUserUpParty upParty, List<Claim> claims, ExternalUser externalUser)
        {
            var userIdClaimType = GetUserIdClaimType(upParty);

            var userIdClaims = claims.Where(c => c.Type == userIdClaimType).Select(c => c.Value);
            if (userIdClaims.Count() > 0)
            {
                claims = claims.Where(c => c.Type != userIdClaimType).ToList();
                foreach (var userIdClaim in userIdClaims)
                {
                    claims.Add(new Claim(userIdClaimType, $"{upParty.Name}|{userIdClaim}"));
                }
            }

            claims.AddClaim(userIdClaimType, externalUser.UserId);
            return claims;
        }

        private string GetUserIdClaimType(ExternalUserUpParty upParty)
        {
            switch (upParty.Type)
            {
                case PartyTypes.Oidc:
                case PartyTypes.TrackLink:
                    return Constants.JwtClaimTypes.LocalSub;
                case PartyTypes.Saml2:
                    return Constants.SamlClaimTypes.LocalNameIdentifier;
                default:
                    throw new NotSupportedException();
            }
        }

        private string GetLinkClaim(ExternalUserUpParty upParty, IEnumerable<Claim> claims) => claims.Where(c => c.Type.Equals(upParty.LinkExternalUser.LinkClaimType, StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).FirstOrDefault();
    }
}
