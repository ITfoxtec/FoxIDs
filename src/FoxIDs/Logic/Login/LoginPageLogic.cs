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
using FoxIDs.Models.Logic;

namespace FoxIDs.Logic
{
    public class LoginPageLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly LoginUpLogic loginUpLogic;

        public LoginPageLogic(TelemetryScopedLogger logger, SequenceLogic sequenceLogic, SessionLoginUpPartyLogic sessionLogic, ClaimTransformLogic claimTransformLogic, LoginUpLogic loginUpLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.sequenceLogic = sequenceLogic;
            this.sessionLogic = sessionLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.loginUpLogic = loginUpLogic;
        }

        public void CheckUpParty(UpSequenceData sequenceData)
        {
            if (!sequenceData.UpPartyId.Equals(RouteBinding.UpParty.Id, StringComparison.Ordinal))
            {
                throw new Exception("Invalid up-party id.");
            }
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            if (RouteBinding.UpParty.Type != PartyTypes.Login)
            {
                throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
            }
        }

        public bool GetRequereMfa(User user, LoginUpParty loginUpParty, LoginUpSequenceData sequenceData)
        {
            if (user.RequireMultiFactor)
            {
                return true;
            }
            else if (loginUpParty.RequireTwoFactor)
            {
                return true;
            }
            else if (sequenceData.Acr?.Where(v => v.Equals(Constants.Oidc.Acr.Mfa, StringComparison.Ordinal))?.Count() > 0)
            {
                return true;
            }

            return false;
        }

        public DownPartySessionLink GetDownPartyLink(UpParty upParty, LoginUpSequenceData sequenceData) => upParty.DisableSingleLogout ? null : sequenceData.DownPartyLink;

        public async Task<IActionResult> LoginResponseSequenceAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, User user, IEnumerable<string> authMethods = null, LoginResponseSequenceSteps fromStep = LoginResponseSequenceSteps.FromEmailVerificationStep)
        {
            var session = await ValidateSessionAndRequestedUserAsync(sequenceData, loginUpParty, user);

            sequenceData.Email = user.Email;
            sequenceData.EmailVerified = user.EmailVerified;
            sequenceData.AuthMethods = authMethods ?? new[] { IdentityConstants.AuthenticationMethodReferenceValues.Pwd };
            if (fromStep <= LoginResponseSequenceSteps.FromEmailVerificationStep && user.ConfirmAccount && !user.EmailVerified)
            {
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.ActionController, Constants.Endpoints.EmailConfirmation, includeSequence: true).ToRedirectResult();
            }
            else if (fromStep <= LoginResponseSequenceSteps.FromMfaStep && GetRequereMfa(user, loginUpParty, sequenceData))
            {
                if (!user.EmailVerified)
                {
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.ActionController, Constants.Endpoints.EmailConfirmation, includeSequence: true).ToRedirectResult();
                }

                if (user.TwoFactorAppSecretExternalName.IsNullOrEmpty())
                {
                    sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.DoRegistration;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.RegisterTwoFactor, includeSequence: true).ToRedirectResult();
                }
                else
                {
                    sequenceData.TwoFactorAppSecretExternalName = user.TwoFactorAppSecretExternalName;
                    sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.Validate;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.TwoFactor, includeSequence: true).ToRedirectResult();
                }
            }
            else
            {
                return await LoginResponseAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData), user, sequenceData.AuthMethods, session: session);
            }
        }

        private async Task<SessionLoginUpPartyCookie> ValidateSessionAndRequestedUserAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, User user)
        {
            var session = await sessionLogic.GetSessionAsync(loginUpParty);
            if (session != null && user.UserId != session.UserId)
            {
                logger.ScopeTrace(() => "Authenticated user and session user do not match.");
                // TODO invalid user login
                throw new NotImplementedException("Authenticated user and session user do not match.");
            }

            if (!sequenceData.UserId.IsNullOrEmpty() && user.UserId != sequenceData.UserId)
            {
                logger.ScopeTrace(() => "Authenticated user and requested user do not match.");
                // TODO invalid user login
                throw new NotImplementedException("Authenticated user and requested user do not match.");
            }

            return session;
        }

        private async Task<IActionResult> LoginResponseAsync(LoginUpParty loginUpParty, DownPartySessionLink newDownPartyLink, User user, IEnumerable<string> authMethods, IEnumerable<Claim> acrClaims = null, SessionLoginUpPartyCookie session = null)
        {
            var authTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List<Claim> claims;
            if (session != null && await sessionLogic.UpdateSessionAsync(loginUpParty, newDownPartyLink, session, acrClaims))
            {
                claims = session.Claims.ToClaimList();
            }
            else
            {
                var sessionId = RandomGenerator.Generate(24);
                claims = await GetClaimsAsync(loginUpParty, user, authTime, authMethods, sessionId, acrClaims);
                await sessionLogic.CreateSessionAsync(loginUpParty, newDownPartyLink, authTime, claims);
            }

            return await loginUpLogic.LoginResponseAsync(claims);
        }

        public async Task<IActionResult> LoginResponseUpdateSessionAsync(LoginUpParty loginUpParty, DownPartySessionLink newDownPartyLink, SessionLoginUpPartyCookie session)
        {
            if (session != null && await sessionLogic.UpdateSessionAsync(loginUpParty, newDownPartyLink, session))
            {
                return await loginUpLogic.LoginResponseAsync(session.Claims.ToClaimList());
            }
            else
            {
                throw new InvalidOperationException("Session do not exist or can not be updated.");
            }
        }

        private async Task<List<Claim>> GetClaimsAsync(LoginUpParty party, User user, long authTime, IEnumerable<string> authMethods, string sessionId, IEnumerable<Claim> acrClaims = null)
        {
            var claims = new List<Claim>();
            claims.AddClaim(JwtClaimTypes.Subject, user.UserId);
            claims.AddClaim(JwtClaimTypes.AuthTime, authTime.ToString());
            claims.AddRange(authMethods.Select(am => new Claim(JwtClaimTypes.Amr, am)));
            if (acrClaims?.Count() > 0)
            {
                claims.AddRange(acrClaims);
            }
            claims.AddClaim(JwtClaimTypes.SessionId, sessionId);
            claims.AddClaim(JwtClaimTypes.PreferredUsername, user.Email);
            claims.AddClaim(JwtClaimTypes.Email, user.Email);
            claims.AddClaim(JwtClaimTypes.EmailVerified, user.EmailVerified.ToString().ToLower());
            claims.AddClaim(Constants.JwtClaimTypes.UpParty, party.Name);
            claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, party.Type.ToString().ToLower());
            if (user.Claims?.Count() > 0)
            {
                claims.AddRange(user.Claims.ToClaimList());
            }
            logger.ScopeTrace(() => $"Up, Login created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            logger.ScopeTrace(() => $"Up, Login output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return transformedClaims;
        }

        public async Task<List<Claim>> GetCreateUserTransformedClaimsAsync(LoginUpParty party, List<Claim> claims)
        {
            logger.ScopeTrace(() => $"Up, Create user created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            var transformedClaims = await claimTransformLogic.Transform(party.CreateUser.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            logger.ScopeTrace(() => $"Up, Create user output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return transformedClaims;
        }
    }
}
