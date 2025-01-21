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

        private bool SupportTwoFactorSms(User user, LoginUpParty loginUpParty) => !user.Phone.IsNullOrEmpty() && !user.DisableTwoFactorSms && !loginUpParty.DisableTwoFactorSms ? true : false;

        private bool SupportTwoFactorEmail(User user, LoginUpParty loginUpParty) => !user.Email.IsNullOrEmpty() && !user.DisableTwoFactorEmail && !loginUpParty.DisableTwoFactorEmail ? true : false;

        public DownPartySessionLink GetDownPartyLink(UpParty upParty, ILoginUpSequenceDataBase sequenceData) => upParty.DisableSingleLogout ? null : sequenceData.DownPartyLink;

        public async Task<IActionResult> LoginResponseSequenceAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, User user, IEnumerable<string> authMethods = null, LoginResponseSequenceSteps fromStep = LoginResponseSequenceSteps.FromPhoneVerificationStep) 
        {
            var session = await ValidateSessionAndRequestedUserAsync(sequenceData, loginUpParty, user.UserId);

            sequenceData.Email = user.Email;
            sequenceData.EmailVerified = user.EmailVerified;
            sequenceData.Phone = user.Phone;
            sequenceData.PhoneVerified = user.PhoneVerified;
            sequenceData.AuthMethods = authMethods ?? [IdentityConstants.AuthenticationMethodReferenceValues.Pwd];
            sequenceData.SupportTwoFactorApp = SupportTwoFactorApp(user, loginUpParty);
            sequenceData.SupportTwoFactorSms = SupportTwoFactorSms(user, loginUpParty);
            sequenceData.SupportTwoFactorEmail = SupportTwoFactorEmail(user, loginUpParty);
            if (fromStep <= LoginResponseSequenceSteps.FromPhoneVerificationStep && user.ConfirmAccount && !user.Phone.IsNullOrEmpty() && !user.PhoneVerified && await PlanEnabledSmsAsync())
            {
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.ActionController, Constants.Endpoints.PhoneConfirmation, includeSequence: true).ToRedirectResult();
            }
            else if (fromStep <= LoginResponseSequenceSteps.FromEmailVerificationStep && user.ConfirmAccount && !user.Email.IsNullOrEmpty() && !user.EmailVerified)
            {
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.ActionController, Constants.Endpoints.EmailConfirmation, includeSequence: true).ToRedirectResult();
            }
            else if (fromStep <= LoginResponseSequenceSteps.FromMfaAllAndAppStep && GetRequireMfa(user, loginUpParty, sequenceData))
            {
                if (fromStep == LoginResponseSequenceSteps.FromMfaSmsStep && sequenceData.SupportTwoFactorSms)
                {
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.SmsTwoFactor, includeSequence: true).ToRedirectResult();
                }
                else if (fromStep == LoginResponseSequenceSteps.FromMfaEmailStep && sequenceData.SupportTwoFactorEmail)
                {
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.EmailTwoFactor, includeSequence: true).ToRedirectResult();
                }
                else
                {
                    if (RegisterTwoFactor(user))
                    {
                        if (sequenceData.SupportTwoFactorSms)
                        {
                            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.SmsTwoFactor, includeSequence: true).ToRedirectResult();
                        }
                        else if (sequenceData.SupportTwoFactorEmail)
                        {
                            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.EmailTwoFactor, includeSequence: true).ToRedirectResult();
                        }
                        else if(sequenceData.SupportTwoFactorApp)
                        {
                            sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.DoRegistration;
                            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.AppTwoFactorRegister, includeSequence: true).ToRedirectResult();
                        }
                    }
                    else
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
                }

                throw new Exception("Require two-factor (2FA/MFA) but it is either not supported or configured.");
            }

            return await LoginResponseAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData), user, sequenceData, session: session);
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

        private bool RegisterTwoFactor(User user)
        {
            if (user.TwoFactorAppSecret.IsNullOrEmpty())
            {
                if (settings.Options.KeyStorage == KeyStorageOptions.KeyVault)
                {
                    return user.TwoFactorAppSecretExternalName.IsNullOrEmpty();
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
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

        private async Task<IActionResult> LoginResponseAsync(LoginUpParty loginUpParty, DownPartySessionLink newDownPartyLink, User user, LoginUpSequenceData sequenceData, IEnumerable<Claim> acrClaims = null, SessionLoginUpPartyCookie session = null)
        {
            var authTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List<Claim> claims;
            if (session != null && await sessionLogic.UpdateSessionAsync(loginUpParty, newDownPartyLink, session, GetLoginUserIdentifier(user, sequenceData.UserIdentifier), acrClaims))
            {
                claims = session.Claims.ToClaimList();
            }
            else
            {
                var sessionId = RandomGenerator.Generate(24);
                claims = await GetClaimsAsync(loginUpParty, user, authTime, sequenceData, sessionId, acrClaims);
                await sessionLogic.CreateSessionAsync(loginUpParty, newDownPartyLink, authTime, GetLoginUserIdentifier(user, sequenceData.UserIdentifier), claims);
            }

            return await serviceProvider.GetService<LoginUpLogic>().LoginResponseAsync(claims);
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

        private async Task<List<Claim>> GetClaimsAsync(LoginUpParty loginUpParty, User user, long authTime, LoginUpSequenceData sequenceData, string sessionId, IEnumerable<Claim> acrClaims = null)
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

            var transformedClaims = await claimTransformLogic.TransformAsync((loginUpParty as IOAuthClaimTransforms)?.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            logger.ScopeTrace(() => $"AuthMethod, Login output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return transformedClaims;
        }

        public async Task<List<Claim>> GetCreateUserTransformedClaimsAsync(LoginUpParty party, List<Claim> claims)
        {
            logger.ScopeTrace(() => $"AuthMethod, Create user created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            var transformedClaims = await claimTransformLogic.TransformAsync(party.CreateUser.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            logger.ScopeTrace(() => $"AuthMethod, Create user output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return transformedClaims;
        }
    }
}
