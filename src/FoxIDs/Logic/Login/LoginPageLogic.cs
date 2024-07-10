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
using FoxIDs.Models.Config;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Logic
{
    public class LoginPageLogic : LogicSequenceBase
    {
        private readonly Settings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly ClaimTransformLogic claimTransformLogic;

        public LoginPageLogic(Settings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, SessionLoginUpPartyLogic sessionLogic, ClaimTransformLogic claimTransformLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
            this.sessionLogic = sessionLogic;
            this.claimTransformLogic = claimTransformLogic;
        }

        public void CheckUpParty(UpSequenceData sequenceData, PartyTypes partyType = PartyTypes.Login)
        {
            if (!sequenceData.UpPartyId.Equals(RouteBinding.UpParty.Id, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            if (RouteBinding.UpParty.Type != partyType)
            {
                throw new NotSupportedException($"Connection type '{RouteBinding.UpParty.Type}' not supported, expecting '{partyType}'.");
            }
        }

        public bool GetRequereMfa(User user, LoginUpParty loginUpParty, ILoginUpSequenceDataBase sequenceData)
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

        public DownPartySessionLink GetDownPartyLink(UpParty upParty, ILoginUpSequenceDataBase sequenceData) => upParty.DisableSingleLogout ? null : sequenceData.DownPartyLink;

        public async Task<IActionResult> LoginResponseSequenceAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, User user, IEnumerable<string> authMethods = null, LoginResponseSequenceSteps fromStep = LoginResponseSequenceSteps.FromEmailVerificationStep) 
        {
            var session = await ValidateSessionAndRequestedUserAsync(sequenceData, loginUpParty, user.Id);

            sequenceData.Email = user.Email;
            sequenceData.EmailVerified = user.EmailVerified;
            sequenceData.AuthMethods = authMethods ?? [IdentityConstants.AuthenticationMethodReferenceValues.Pwd];
            if (fromStep <= LoginResponseSequenceSteps.FromEmailVerificationStep && user.ConfirmAccount && !user.EmailVerified)
            {
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.ActionController, Constants.Endpoints.EmailConfirmation, includeSequence: true).ToRedirectResult(RouteBinding.DisplayName);
            }
            else if (fromStep <= LoginResponseSequenceSteps.FromMfaStep && GetRequereMfa(user, loginUpParty, sequenceData))
            {
                if (!user.EmailVerified)
                {
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.ActionController, Constants.Endpoints.EmailConfirmation, includeSequence: true).ToRedirectResult(RouteBinding.DisplayName);
                }

                if (RegisterTwoFactor(user))
                {
                    sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.DoRegistration;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.RegisterTwoFactor, includeSequence: true).ToRedirectResult(RouteBinding.DisplayName);
                }
                else
                {
                    if (settings.Options.KeyStorage == KeyStorageOptions.None)
                    {
                        sequenceData.TwoFactorAppSecretOrExtName = user.TwoFactorAppSecret;
                    }
                    else if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault)
                    {
                        sequenceData.TwoFactorAppSecretOrExtName = user.TwoFactorAppSecretExternalName;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.Validate;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.TwoFactor, includeSequence: true).ToRedirectResult(RouteBinding.DisplayName);
                }
            }
            else
            {
                return await LoginResponseAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData), user, sequenceData.AuthMethods, session: session);
            }
        }

        private bool RegisterTwoFactor(User user)
        {
            if (settings.Options.KeyStorage == KeyStorageOptions.None)
            {
                return user.TwoFactorAppSecret.IsNullOrEmpty();
            }
            else if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault)
            {
                return user.TwoFactorAppSecretExternalName.IsNullOrEmpty();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private async Task<SessionLoginUpPartyCookie> GetSessionAsync(ILoginUpSequenceDataBase sequenceData, IUpParty upParty)
        {
            if (sequenceData.LoginAction == LoginAction.RequireLogin)
            {
                return null;
            }

            return await sessionLogic.GetSessionAsync(upParty);
        }

        public async Task<SessionLoginUpPartyCookie> ValidateSessionAndRequestedUserAsync(ILoginUpSequenceDataBase sequenceData, IUpParty upParty, string userId)
        {
            var session = await GetSessionAsync(sequenceData, upParty);
            if (session != null && userId != session.UserId)
            {
                logger.ScopeTrace(() => "Authenticated user and session user do not match.");
                // TODO invalid user login
                throw new NotImplementedException("Authenticated user and session user do not match.");
            }

            if (!sequenceData.UserId.IsNullOrEmpty() && userId != sequenceData.UserId)
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

            return await serviceProvider.GetService<LoginUpLogic>().LoginResponseAsync(claims);
        }

        public bool ValidSessionUpAgainstSequence(ILoginUpSequenceDataBase sequenceData, SessionLoginUpPartyCookie session, bool requereMfa = false)
        {
            if (session == null) return false;

            if (sequenceData.MaxAge.HasValue && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - session.CreateTime > sequenceData.MaxAge.Value)
            {
                logger.ScopeTrace(() => $"Session max age not accepted, Max age '{sequenceData.MaxAge}', Session created '{session.CreateTime}'.");
                return false;
            }

            if (!sequenceData.UserId.IsNullOrWhiteSpace() && !session.UserId.Equals(sequenceData.UserId, StringComparison.OrdinalIgnoreCase))
            {
                logger.ScopeTrace(() => $"Session user '{session.UserId}' and requested user '{sequenceData.UserId}' do not match.");
                return false;
            }

            if (requereMfa && !(session.Claims?.Where(c => c.Claim == JwtClaimTypes.Amr && c.Values.Where(v => v == IdentityConstants.AuthenticationMethodReferenceValues.Mfa).Any())?.Count() > 0))
            {
                logger.ScopeTrace(() => "Session does not meet the MFA requirement.");
                return false;
            }

            return true;
        }

        public async Task<IActionResult> LoginResponseUpdateSessionAsync(LoginUpParty loginUpParty, DownPartySessionLink newDownPartyLink, SessionLoginUpPartyCookie session)
        {
            if (session != null && await sessionLogic.UpdateSessionAsync(loginUpParty, newDownPartyLink, session))
            {
                return await serviceProvider.GetService<LoginUpLogic>().LoginResponseAsync(session.Claims.ToClaimList());
            }
            else
            {
                throw new InvalidOperationException("Session do not exist or can not be updated.");
            }
        }

        private async Task<List<Claim>> GetClaimsAsync(LoginUpParty loginUpParty, User user, long authTime, IEnumerable<string> authMethods, string sessionId, IEnumerable<Claim> acrClaims = null)
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
            claims.AddClaim(Constants.JwtClaimTypes.AuthMethod, loginUpParty.Name);
            claims.AddClaim(Constants.JwtClaimTypes.AuthMethodType, loginUpParty.Type.GetPartyTypeValue());
            claims.AddClaim(Constants.JwtClaimTypes.UpParty, loginUpParty.Name);
            claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, loginUpParty.Type.GetPartyTypeValue());
            if (user.Claims?.Count() > 0)
            {
                claims.AddRange(user.Claims.ToClaimList());
            }
            logger.ScopeTrace(() => $"AuthMethod, Login created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var transformedClaims = await claimTransformLogic.Transform((loginUpParty as IOAuthClaimTransforms)?.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            logger.ScopeTrace(() => $"AuthMethod, Login output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return transformedClaims;
        }

        public async Task<List<Claim>> GetCreateUserTransformedClaimsAsync(LoginUpParty party, List<Claim> claims)
        {
            logger.ScopeTrace(() => $"AuthMethod, Create user created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            var transformedClaims = await claimTransformLogic.Transform(party.CreateUser.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            logger.ScopeTrace(() => $"AuthMethod, Create user output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return transformedClaims;
        }
    }
}
