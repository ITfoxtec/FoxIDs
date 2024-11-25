using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using FoxIDs.Models.Sequences;
using System.Linq;
using FoxIDs.Models.Session;
using ITfoxtec.Identity.Util;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Logic
{
    public class ExternalLoginPageLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly LoginPageLogic loginPageLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ClaimValidationLogic claimValidationLogic;

        public ExternalLoginPageLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, SessionLoginUpPartyLogic sessionLogic, LoginPageLogic loginPageLogic, ClaimTransformLogic claimTransformLogic, ClaimValidationLogic claimValidationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
            this.sessionLogic = sessionLogic;
            this.loginPageLogic = loginPageLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.claimValidationLogic = claimValidationLogic;
        }

        public void CheckUpParty(ExternalLoginUpSequenceData sequenceData, PartyTypes partyType) => loginPageLogic.CheckUpParty(sequenceData, partyType);

        public DownPartySessionLink GetDownPartyLink(ExternalLoginUpParty upParty, ExternalLoginUpSequenceData sequenceData) => loginPageLogic.GetDownPartyLink(upParty, sequenceData);

        public async Task<IActionResult> LoginResponseSequenceAsync(ExternalLoginUpSequenceData sequenceData, ExternalLoginUpParty extLoginUpParty, IEnumerable<Claim> claims, IEnumerable<string> authMethods = null)
        {
            var userId = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject);
            var session = await loginPageLogic.ValidateSessionAndRequestedUserAsync(sequenceData, extLoginUpParty, userId);

            sequenceData.Claims = claims.ToClaimAndValues();
            sequenceData.AuthMethods = authMethods ?? [IdentityConstants.AuthenticationMethodReferenceValues.Pwd];
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);

            return await LoginResponseAsync(extLoginUpParty, loginPageLogic.GetDownPartyLink(extLoginUpParty, sequenceData), claims, sequenceData, session: session);
        }

        private async Task<IActionResult> LoginResponseAsync(ExternalLoginUpParty extLoginUpParty, DownPartySessionLink newDownPartyLink, IEnumerable<Claim> userClaims, ExternalLoginUpSequenceData sequenceData, IEnumerable<Claim> acrClaims = null, SessionLoginUpPartyCookie session = null)
        {
            List<Claim> claims;
            if (session != null && await sessionLogic.UpdateSessionAsync(extLoginUpParty, newDownPartyLink, session, acrClaims))
            {
                claims = session.Claims.ToClaimList();
            }
            else
            {
                var authTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var sessionId = RandomGenerator.Generate(24);
                claims = await GetClaimsAsync(extLoginUpParty, userClaims, authTime, sequenceData, sessionId, acrClaims);

                await sessionLogic.CreateSessionAsync(extLoginUpParty, newDownPartyLink, authTime, claims);
            }

            return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginResponseAsync(extLoginUpParty, claims);
        }

        public bool ValidSessionUpAgainstSequence(ExternalLoginUpSequenceData sequenceData, SessionLoginUpPartyCookie session) => loginPageLogic.ValidSessionUpAgainstSequence(sequenceData, session);

        public async Task<IActionResult> LoginResponseUpdateSessionAsync(ExternalLoginUpParty extLoginUpParty, DownPartySessionLink newDownPartyLink, SessionLoginUpPartyCookie session)
        {
            if (session != null && await sessionLogic.UpdateSessionAsync(extLoginUpParty, newDownPartyLink, session))
            {
                return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginResponseAsync(extLoginUpParty, session.Claims.ToClaimList());
            }
            else
            {
                throw new InvalidOperationException("Session do not exist or can not be updated.");
            }
        }

        private async Task<List<Claim>> GetClaimsAsync(ExternalLoginUpParty extLoginUpParty, IEnumerable<Claim> userClaims, long authTime, ExternalLoginUpSequenceData sequenceData, string sessionId, IEnumerable<Claim> acrClaims = null)
        {
            var subject = userClaims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject);

            var claims = userClaims.Where(c => c.Type != JwtClaimTypes.Subject && c.Type != JwtClaimTypes.SessionId &&
                c.Type != Constants.JwtClaimTypes.AuthMethod && c.Type != Constants.JwtClaimTypes.AuthProfileMethod && c.Type != Constants.JwtClaimTypes.AuthMethodType &&
                c.Type != Constants.JwtClaimTypes.UpParty && c.Type != Constants.JwtClaimTypes.UpPartyType).ToList();

            claims.AddClaim(JwtClaimTypes.Subject, $"{extLoginUpParty.Name}|{subject}");
            claims.AddClaim(JwtClaimTypes.AuthTime, authTime.ToString());
            claims.AddRange(sequenceData.AuthMethods.Select(am => new Claim(JwtClaimTypes.Amr, am)));
            if (acrClaims?.Count() > 0)
            {
                claims.AddRange(acrClaims);
            }
            claims.AddClaim(JwtClaimTypes.SessionId, sessionId);
            claims.AddClaim(Constants.JwtClaimTypes.AuthMethod, extLoginUpParty.Name);
            if (!sequenceData.UpPartyProfileName.IsNullOrEmpty())
            {
                claims.AddClaim(Constants.JwtClaimTypes.AuthProfileMethod, sequenceData.UpPartyProfileName);
            }
            claims.AddClaim(Constants.JwtClaimTypes.AuthMethodType, extLoginUpParty.Type.GetPartyTypeValue());
            claims.AddClaim(Constants.JwtClaimTypes.UpParty, extLoginUpParty.Name);
            claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, extLoginUpParty.Type.GetPartyTypeValue());
            logger.ScopeTrace(() => $"AuthMethod, External login, with added JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var transformedClaims = await claimTransformLogic.TransformAsync((extLoginUpParty as IOAuthClaimTransforms)?.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);

            var validClaims = claimValidationLogic.ValidateUpPartyClaims(extLoginUpParty.Claims, transformedClaims);
            logger.ScopeTrace(() => $"AuthMethod, External login, transformed JWT claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return validClaims;
        }
    }
}
