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
using FoxIDs.Repository;

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
        private readonly PlanCacheLogic planCacheLogic;

        public LoginPageLogic(Settings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, SessionLoginUpPartyLogic sessionLogic, ClaimTransformLogic claimTransformLogic, PlanCacheLogic planCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
            this.sessionLogic = sessionLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.planCacheLogic = planCacheLogic;
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

        public bool GetRequireMfa(User user, LoginUpParty loginUpParty, ILoginUpSequenceDataBase sequenceData)
        {
            if (user.RequireMultiFactor)
            {
                return UserAndLoginUpPartySupportMultiFactor(user, loginUpParty);
            }
            else if (loginUpParty.RequireTwoFactor)
            {
                return UserAndLoginUpPartySupportMultiFactor(user, loginUpParty);
            }
            else if (sequenceData.Acr?.Where(v => v.Equals(Constants.Oidc.Acr.Mfa, StringComparison.Ordinal))?.Count() > 0)
            {
                return UserAndLoginUpPartySupportMultiFactor(user, loginUpParty);
            }

            return false;
        }

        private bool UserAndLoginUpPartySupportMultiFactor(User user, LoginUpParty loginUpParty)
        {
            if(!(user.DisableTwoFactorApp && user.DisableTwoFactorSms && user.DisableTwoFactorEmail) && !(loginUpParty.DisableTwoFactorApp && loginUpParty.DisableTwoFactorSms && loginUpParty.DisableTwoFactorEmail))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool SupportTwoFactorApp(User user, LoginUpParty loginUpParty) => !user.DisableTwoFactorApp && !loginUpParty.DisableTwoFactorApp ? true : false;

        private bool SupportRegisterTwoFactorApp(User user, LoginUpParty loginUpParty) => user.Phone.IsNullOrEmpty() && !user.Email.IsNullOrEmpty();

        private bool SupportTwoFactorSms(User user, LoginUpParty loginUpParty) => !user.Phone.IsNullOrEmpty() && !user.DisableTwoFactorSms && !loginUpParty.DisableTwoFactorSms ? true : false;

        private bool SupportTwoFactorEmail(User user, LoginUpParty loginUpParty) => !user.Email.IsNullOrEmpty() && !user.DisableTwoFactorEmail && !loginUpParty.DisableTwoFactorEmail ? true : false;

        public DownPartySessionLink GetDownPartyLink(UpParty upParty, ILoginUpSequenceDataBase sequenceData) => upParty.DisableSingleLogout ? null : sequenceData.DownPartyLink;

        public async Task<IActionResult> LoginResponseSequenceAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, User user, IEnumerable<string> authMethods = null, LoginResponseSequenceSteps step = LoginResponseSequenceSteps.PhoneVerificationStep) 
        {
            try
            {
                var session = await ValidateSessionAndRequestedUserAsync(sequenceData, loginUpParty, user.UserId);

                sequenceData.Email = user.Email;
                sequenceData.EmailVerified = user.EmailVerified;
                sequenceData.Phone = user.Phone;
                sequenceData.PhoneVerified = user.PhoneVerified;
                sequenceData.AuthMethods = authMethods ?? [IdentityConstants.AuthenticationMethodReferenceValues.Pwd];

                if (step <= LoginResponseSequenceSteps.PhoneVerificationStep && user.ConfirmAccount && !user.Phone.IsNullOrEmpty() && !user.PhoneVerified && await PlanEnabledSmsAsync())
                {
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.ActionController, Constants.Endpoints.PhoneConfirmation, includeSequence: true).ToRedirectResult();
                }
                else if (step <= LoginResponseSequenceSteps.EmailVerificationStep && user.ConfirmAccount && !user.Email.IsNullOrEmpty() && !user.EmailVerified)
                {
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.ActionController, Constants.Endpoints.EmailConfirmation, includeSequence: true).ToRedirectResult();
                }
                else if (step <= LoginResponseSequenceSteps.MfaAllAndAppStep && GetRequireMfa(user, loginUpParty, sequenceData))
                {
                    sequenceData.SupportTwoFactorApp = SupportTwoFactorApp(user, loginUpParty);
                    sequenceData.TwoFactorAppIsRegistred = TwoFactorAppIsRegistred(user);
                    sequenceData.SupportTwoFactorSms = SupportTwoFactorSms(user, loginUpParty);
                    sequenceData.SupportTwoFactorEmail = SupportTwoFactorEmail(user, loginUpParty);

                    if (step == LoginResponseSequenceSteps.MfaSmsStep)
                    {
                        if (sequenceData.SupportTwoFactorSms)
                        {
                            return await SmsTwoFactorResponseAsync(sequenceData, loginUpParty);
                        }
                    }
                    else if (step == LoginResponseSequenceSteps.MfaEmailStep)
                    {
                        if (sequenceData.SupportTwoFactorEmail)
                        {
                            return await EmailTwoFactorResponseAsync(sequenceData, loginUpParty);
                        }
                    }
                    else if (step == LoginResponseSequenceSteps.MfaRegisterAuthAppStep)
                    {
                        if (sequenceData.SupportTwoFactorApp)
                        {
                            return await AuthAppTwoFactorRegistrationResponseAsync(sequenceData, loginUpParty);
                        }
                    }
                    else
                    {
                        if (sequenceData.SupportTwoFactorApp && sequenceData.TwoFactorAppIsRegistred)
                        {
                            return await AuthAppTwoFactorResponseAsync(sequenceData, loginUpParty, user);
                        }

                        if (sequenceData.SupportTwoFactorSms)
                        {
                            return await SmsTwoFactorResponseAsync(sequenceData, loginUpParty);
                        }

                        if (sequenceData.SupportTwoFactorEmail)
                        {
                            return await EmailTwoFactorResponseAsync(sequenceData, loginUpParty);
                        }

                        if (sequenceData.SupportTwoFactorApp && SupportRegisterTwoFactorApp(user, loginUpParty))
                        {
                            return await AuthAppTwoFactorRegistrationResponseAsync(sequenceData, loginUpParty);
                        }
                    }

                    throw new Exception($"Require two-factor (2FA/MFA) but it is either not supported or configured. {nameof(LoginResponseSequenceSteps)}: '{step}'.");
                }

                return await LoginResponseAsync(loginUpParty, sequenceData, GetDownPartyLink(loginUpParty, sequenceData), user, session: session);
            }
            catch (OAuthRequestException orex)
            {
                logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                logger.Error(orex);
                return await serviceProvider.GetService<LoginUpLogic>().LoginResponseErrorAsync(sequenceData, error: orex.Error, errorDescription: orex.ErrorDescription);
            }
        }

        private async Task<IActionResult> AuthAppTwoFactorRegistrationResponseAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty)
        {
            sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.DoRegistration;
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.AppTwoFactorRegister, includeSequence: true).ToRedirectResult();
        }

        private async Task<IActionResult> EmailTwoFactorResponseAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty)
        {
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.EmailTwoFactor, includeSequence: true).ToRedirectResult();
        }

        private async Task<IActionResult> SmsTwoFactorResponseAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty)
        {
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.SmsTwoFactor, includeSequence: true).ToRedirectResult();
        }

        private async Task<IActionResult> AuthAppTwoFactorResponseAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, User user)
        {
            if (!user.TwoFactorAppSecret.IsNullOrWhiteSpace())
            {
                sequenceData.TwoFactorAppSecret = user.TwoFactorAppSecret;
            }
            else if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault)
            {
                var externalSecretLogic = serviceProvider.GetService<ExternalSecretLogic>();
                sequenceData.TwoFactorAppSecret = user.TwoFactorAppSecret = await externalSecretLogic.GetExternalSecretAsync(user.TwoFactorAppSecretExternalName);
                if (sequenceData.TwoFactorAppSecret.IsNullOrWhiteSpace())
                {
                    throw new InvalidOperationException($"Unable to get external secret from Key Vault, {nameof(user.TwoFactorAppSecretExternalName)} '{user.TwoFactorAppSecretExternalName}'.");
                }
                await externalSecretLogic.DeleteExternalSecretAsync(user.TwoFactorAppSecretExternalName);
                user.TwoFactorAppSecretExternalName = null;
                var tenantDataRepository = serviceProvider.GetService<ITenantDataRepository>();
                await tenantDataRepository.SaveAsync(user);
            }
            sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.Validate;
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.AppTwoFactor, includeSequence: true).ToRedirectResult();
        }

        private async Task<bool> PlanEnabledSmsAsync()
        {
            if (!RouteBinding.PlanName.IsNullOrEmpty())
            {
                var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                return plan.EnableSms;
            }
            return true;
        }

        private bool TwoFactorAppIsRegistred(User user)
        {
            if (user.TwoFactorAppSecret.IsNullOrEmpty())
            {
                if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault)
                {
                    return !user.TwoFactorAppSecretExternalName.IsNullOrEmpty();
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
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
            if (session != null && userId != session.UserIdClaim)
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

        private async Task<IActionResult> LoginResponseAsync(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData, DownPartySessionLink newDownPartyLink, User user, IEnumerable<Claim> acrClaims = null, SessionLoginUpPartyCookie session = null)
        {
            var authTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List<Claim> claims;
            if (session != null && await sessionLogic.UpdateSessionAsync(loginUpParty, session, GetLoginUserIdentifier(user, sequenceData.UserIdentifier), acrClaims))
            {
                claims = session.Claims.ToClaimList();
            }
            else
            {
                var sessionId = RandomGenerator.Generate(24);
                (claims, var actionResult) = await GetClaimsAsync(loginUpParty, sequenceData, newDownPartyLink, user, authTime, sessionId, acrClaims);
                if (actionResult != null)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
                    return actionResult;
                }
                await sessionLogic.CreateSessionAsync(loginUpParty, authTime, GetLoginUserIdentifier(user, sequenceData.UserIdentifier), claims);
            }

            return await serviceProvider.GetService<LoginUpLogic>().LoginResponseAsync(sequenceData, claims);
        }

        private LoginUserIdentifier GetLoginUserIdentifier(User user, string userIdentifier)
        {
            return new LoginUserIdentifier
            {
                UserId = user.UserId,
                Email = user.Email,
                Phone = user.Phone,
                Username = user.Username,
                UserIdentifier = userIdentifier
            };
        }

        public bool ValidSessionUpAgainstSequence(ILoginUpSequenceDataBase sequenceData, SessionLoginUpPartyCookie session, bool requereMfa = false)
        {
            if (session == null) return false;

            if (sequenceData.MaxAge.HasValue && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - session.CreateTime > sequenceData.MaxAge.Value)
            {
                logger.ScopeTrace(() => $"Session max age not accepted, Max age '{sequenceData.MaxAge}', Session created '{session.CreateTime}'.");
                return false;
            }

            if (!sequenceData.UserId.IsNullOrWhiteSpace() && !session.UserIdClaim.Equals(sequenceData.UserId, StringComparison.OrdinalIgnoreCase))
            {
                logger.ScopeTrace(() => $"Session user '{session.UserIdClaim}' and requested user '{sequenceData.UserId}' do not match.");
                return false;
            }

            if (requereMfa && !(session.Claims?.Where(c => c.Claim == JwtClaimTypes.Amr && c.Values.Where(v => v == IdentityConstants.AuthenticationMethodReferenceValues.Mfa).Any())?.Count() > 0))
            {
                logger.ScopeTrace(() => "Session does not meet the MFA requirement.");
                return false;
            }

            return true;
        }

        public async Task<IActionResult> LoginResponseUpdateSessionAsync(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData, SessionLoginUpPartyCookie session)
        {
            if (session != null && await sessionLogic.UpdateSessionAsync(loginUpParty, session))
            {
                await sessionLogic.AddOrUpdateSessionTrackAsync(loginUpParty, sequenceData.DownPartyLink);
                return await serviceProvider.GetService<LoginUpLogic>().LoginResponseAsync(sequenceData, session.Claims.ToClaimList());
            }
            else
            {
                throw new InvalidOperationException("Session do not exist or can not be updated.");
            }
        }

        private async Task<(List<Claim> claims, IActionResult actionResult)> GetClaimsAsync(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData, DownPartySessionLink newDownPartyLink, User user, long authTime, string sessionId, IEnumerable<Claim> acrClaims = null)
        {
            var claims = new List<Claim>();
            claims.AddClaim(JwtClaimTypes.Subject, user.UserId);
            claims.AddClaim(JwtClaimTypes.AuthTime, authTime.ToString());
            claims.AddRange(sequenceData.AuthMethods.Select(am => new Claim(JwtClaimTypes.Amr, am)));
            if (acrClaims?.Count() > 0)
            {
                claims.AddRange(acrClaims);
            }
            claims.AddClaim(JwtClaimTypes.SessionId, sessionId);
            if (!user.Email.IsNullOrEmpty())
            {
                claims.AddClaim(JwtClaimTypes.Email, user.Email);
                claims.AddClaim(JwtClaimTypes.EmailVerified, user.EmailVerified.ToString().ToLower());
            }
            if (!user.Phone.IsNullOrEmpty())
            {
                claims.AddClaim(JwtClaimTypes.PhoneNumber, user.Phone);
                claims.AddClaim(JwtClaimTypes.PhoneNumberVerified, user.PhoneVerified.ToString().ToLower());
            }
            if (!user.Username.IsNullOrEmpty() || !user.Email.IsNullOrEmpty())
            {
                claims.AddClaim(JwtClaimTypes.PreferredUsername, !user.Username.IsNullOrEmpty() ? user.Username : user.Email);
            }
            claims.AddClaim(Constants.JwtClaimTypes.AuthMethod, loginUpParty.Name);
            if (!sequenceData.UpPartyProfileName.IsNullOrEmpty())
            {
                claims.AddClaim(Constants.JwtClaimTypes.AuthProfileMethod, sequenceData.UpPartyProfileName);
            }
            claims.AddClaim(Constants.JwtClaimTypes.AuthMethodType, loginUpParty.Type.GetPartyTypeValue());
            claims.AddClaim(Constants.JwtClaimTypes.UpParty, loginUpParty.Name);
            claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, loginUpParty.Type.GetPartyTypeValue());
            if (user.Claims?.Count() > 0)
            {
                claims.AddRange(user.Claims.ToClaimList());
            }
            logger.ScopeTrace(() => $"AuthMethod, Login created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            await sessionLogic.CreateOrUpdateMarkerSessionAsync(loginUpParty, newDownPartyLink);

            (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync((loginUpParty as IOAuthClaimTransforms)?.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
            if (actionResult != null)
            {
                return (null, actionResult);
            }
            logger.ScopeTrace(() => $"AuthMethod, Login output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return (transformedClaims, null);
        }

        public async Task<(List<Claim> claims, IActionResult actionResult)> GetCreateUserTransformedClaimsAsync(LoginUpParty party, LoginUpSequenceData sequenceData, List<Claim> claims)
        {
            logger.ScopeTrace(() => $"AuthMethod, Create user created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(party.CreateUser.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
            if (actionResult != null)
            {
                return (null, actionResult);
            }
            logger.ScopeTrace(() => $"AuthMethod, Create user output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return (transformedClaims, null);
        }
    }
}
