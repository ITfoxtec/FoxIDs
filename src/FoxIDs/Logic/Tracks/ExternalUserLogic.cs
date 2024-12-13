using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
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
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly ClaimsDownLogic claimsDownLogic;
        private readonly ClaimTransformLogic claimTransformLogic;

        public ExternalUserLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, ClaimsDownLogic claimsDownLogic, ClaimTransformLogic claimTransformLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.claimsDownLogic = claimsDownLogic;
            this.claimTransformLogic = claimTransformLogic;
        }


        public async Task<(IActionResult externalUserActionResult, IEnumerable<Claim> externalUserClaims)> HandleUserAsync<TProfile>(UpPartyWithExternalUser<TProfile> party, IEnumerable<Claim> claims, Action<ExternalUserUpSequenceData> populateSequenceDataAction, Action<string> requireUserExceptionAction) where TProfile : UpPartyProfile
        {
            if (party.LinkExternalUser == null || (party.LinkExternalUser.LinkClaimType.IsNullOrWhiteSpace() && party.LinkExternalUser.RedemptionClaimType.IsNullOrWhiteSpace()))
            {
                return (null, null);
            }

            var linkClaimValue = GetLinkClaim(GetJwtClaimType(party, party.LinkExternalUser.LinkClaimType), claims);
            logger.ScopeTrace(() => $"Validating external user, link claim type '{party.LinkExternalUser.LinkClaimType}' and value '{linkClaimValue}', Route '{RouteBinding?.Route}'.");
            if (!linkClaimValue.IsNullOrWhiteSpace())
            {
                var externalUser = await tenantDataRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, party.Name, await linkClaimValue.HashIdStringAsync()), required: false);

                if (externalUser == null && !party.LinkExternalUser.RedemptionClaimType.IsNullOrWhiteSpace())
                {
                    var redemptionClaimValue = GetLinkClaim(GetJwtClaimType(party, party.LinkExternalUser.RedemptionClaimType), claims)?.ToLower();
                    logger.ScopeTrace(() => $"Validating external user, redemption claim type '{party.LinkExternalUser.RedemptionClaimType}' and value '{redemptionClaimValue}', Route '{RouteBinding?.Route}'.");
                    if (!redemptionClaimValue.IsNullOrWhiteSpace())
                    {
                        externalUser = await tenantDataRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, party.Name, await redemptionClaimValue.HashIdStringAsync()), required: false);
                        if (externalUser != null)
                        {
                            // Change to use a link claim type instead of redemption claim type.
                            var oldExternalIserId = externalUser.Id;
                            externalUser.Id = await ExternalUser.IdFormatAsync(RouteBinding, party.Name, await linkClaimValue.HashIdStringAsync());
                            externalUser.LinkClaimValue = linkClaimValue;
                            await tenantDataRepository.CreateAsync(externalUser);
                            await tenantDataRepository.DeleteAsync<ExternalUser>(oldExternalIserId);
                        }
                    }
                    else
                    {
                        try
                        {
                            throw new EndpointException($"External user, redemption claim value is empty for link claim type '{party.LinkExternalUser.RedemptionClaimType}'.") { RouteBinding = RouteBinding };
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex);
                        }
                    }
                }

                if (externalUser != null)
                {
                    if (!externalUser.DisableAccount)
                    {
                        var externalUserClaims = GetExternalUserClaim(party, externalUser);
                        logger.ScopeTrace(() => $"AuthMethod, External user output JWT claims '{externalUserClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                        return (null, externalUserClaims);
                    }
                    else
                    {
                        requireUserExceptionAction($"External user is disabled for link claim type '{party.LinkExternalUser.LinkClaimType}' and value '{linkClaimValue}'.");
                    }
                }
                else if (party.LinkExternalUser.AutoCreateUser)
                {
                    if (party.LinkExternalUser.Elements?.Count > 0)
                    {
                        return (await StartUICreateUserAsync(party, linkClaimValue, claims, populateSequenceDataAction), null);
                    }
                    else
                    {
                        return (null, await CreateUserAsync(party, linkClaimValue));
                    }
                }

                if (party.LinkExternalUser.RequireUser)
                {
                    requireUserExceptionAction($"Require external user for link claim type '{party.LinkExternalUser.LinkClaimType}' and value '{linkClaimValue}'{(party.LinkExternalUser.RedemptionClaimType.IsNullOrWhiteSpace() ? string.Empty : $" or redemption claim type '{party.LinkExternalUser.RedemptionClaimType}'")}.");
                }
            }
            else
            {
                try
                {
                    throw new EndpointException($"External user, link claim value is empty for link claim type '{party.LinkExternalUser.LinkClaimType}'.") { RouteBinding = RouteBinding };
                }
                catch (Exception ex)
                {
                    logger.Warning(ex);
                }            
            }

            return (null, null);
        }

        private string GetJwtClaimType<TProfile>(UpPartyWithExternalUser<TProfile> party, string claimType) where TProfile : UpPartyProfile
        {
            if (party.Type == PartyTypes.Saml2)
            {
                var jwtLinkClaimTypes = claimsDownLogic.FromSamlToJwtInfoClaimType(claimType);
                if (jwtLinkClaimTypes.Count() > 0)
                {
                    return jwtLinkClaimTypes.First();
                }
            }
            return claimType;
        }

        public async Task<IEnumerable<Claim>> CreateUserAsync<TProfile>(UpPartyWithExternalUser<TProfile> upParty, string linkClaimValue, IEnumerable<Claim> dynamicElementClaims = null) where TProfile : UpPartyProfile
        {
            logger.ScopeTrace(() => $"Creating external user, link claim value '{linkClaimValue}', Route '{RouteBinding?.Route}'.");

            dynamicElementClaims = dynamicElementClaims ?? new List<Claim>();
            var transformedClaims = await claimTransformLogic.TransformAsync(upParty.LinkExternalUser.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), dynamicElementClaims);

            var externalUser = new ExternalUser
            {
                Id = await ExternalUser.IdFormatAsync(RouteBinding, upParty.Name, await linkClaimValue.HashIdStringAsync()),
                UserId = Guid.NewGuid().ToString(),
                LinkClaimValue = linkClaimValue,
                Claims = transformedClaims.ToClaimAndValues()
            };

            await tenantDataRepository.CreateAsync(externalUser);
            logger.ScopeTrace(() => $"External user created, with user id '{externalUser.UserId}'.");

            var externalUserClaims = GetExternalUserClaim(upParty, externalUser);
            logger.ScopeTrace(() => $"AuthMethod, Created external user output JWT claims '{externalUserClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return externalUserClaims;
        }  

        private async Task<IActionResult> StartUICreateUserAsync<TProfile>(UpPartyWithExternalUser<TProfile> party, string linkClaimValue, IEnumerable<Claim> claims, Action<ExternalUserUpSequenceData> populateSequenceDataAction) where TProfile : UpPartyProfile
        {
            logger.ScopeTrace(() => $"Start UI create external user, link claim '{linkClaimValue}', Route '{RouteBinding?.Route}'.");
            var sequenceData = new ExternalUserUpSequenceData
            {
                UpPartyId = party.Id,
                UpPartyType = party.Type,
                Claims = claims?.ToClaimAndValues(),
                LinkClaimValue = linkClaimValue
            };
            populateSequenceDataAction(sequenceData);
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return HttpContext.GetUpPartyUrl(party.Name, Constants.Routes.ExtController, Constants.Endpoints.CreateUser, includeSequence: true).ToRedirectResult();
        }

        private List<Claim> GetExternalUserClaim<TProfile>(UpPartyWithExternalUser<TProfile> party, ExternalUser externalUser) where TProfile : UpPartyProfile
        {
            var claims = externalUser.Claims?.ToClaimList() ?? new List<Claim>();
            var userIdClaims = claims.Where(c => c.Type == Constants.JwtClaimTypes.LocalSub).Select(c => c.Value);
            if (userIdClaims.Count() > 0)
            {
                claims = claims.Where(c => c.Type != Constants.JwtClaimTypes.LocalSub).ToList();
                foreach (var userIdClaim in userIdClaims)
                {
                    claims.Add(new Claim(Constants.JwtClaimTypes.LocalSub, $"{party.Name}|{userIdClaim}"));
                }
            }

            claims.AddClaim(Constants.JwtClaimTypes.LocalSub, externalUser.UserId);
            return claims;
        }

        private string GetLinkClaim(string linkClaimType, IEnumerable<Claim> claims) => claims.Where(c => c.Type.Equals(linkClaimType, StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).FirstOrDefault();

        public List<Claim> AddExternalUserClaims<TProfile>(UpPartyWithExternalUser<TProfile> party, List<Claim> claims, IEnumerable<Claim> externalUserClaims) where TProfile : UpPartyProfile
        {
            if (externalUserClaims?.Count() > 0)
            {
                claims = party.LinkExternalUser?.OverwriteClaims == true ? externalUserClaims.ConcatOnce(claims).ToList() : externalUserClaims.Concat(claims).ToList();
            }
            return claims;
        }
    }
}
