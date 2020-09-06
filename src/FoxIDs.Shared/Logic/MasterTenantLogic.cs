using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class MasterTenantLogic : LogicBase
    {
        const string loginName = "login";
        const string controlApiResourceName = "foxids_control_api";
        const string controlClientName = "foxids_control_client";

        const string controlApiResourceTenantScope = "foxids_tenant";
        const string controlApiResourceMasterScope = "foxids_master";

        private readonly ITenantRepository tenantService;
        private readonly AccountLogic accountLogic;

        public MasterTenantLogic(ITenantRepository tenantService, AccountLogic accountLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tenantService = tenantService;
            this.accountLogic = accountLogic;
        }

        public async Task CreateMasterTrackDocumentAsync(string tenantName)
        {

            var mTrack = new Track
            {
                Name = Constants.Routes.MasterTrackName,
                SequenceLifetime = 600,
                PasswordLength = 8,
                CheckPasswordComplexity = true,
                CheckPasswordRisk = true
            };
            await mTrack.SetIdAsync(new Track.IdKey { TenantName = tenantName?.ToLower(), TrackName = Constants.Routes.MasterTrackName });

            var certificate = await $"{tenantName?.ToLower()}.{mTrack.Name}".CreateSelfSignedCertificateAsync();
            mTrack.PrimaryKey = new TrackKey()
            {
                Type = TrackKeyType.Contained,
                Key = await certificate.ToJsonWebKeyAsync(true)
            };

            await tenantService.CreateAsync(mTrack);
        }

        public async Task<LoginUpParty> CreateLoginDocumentAsync(string tenantName)
        {
            var mLoginUpParty = new LoginUpParty
            {
                Name = loginName,
                EnableCreateUser = false,
                EnableCancelLogin = false,
                SessionLifetime = 0,
                PersistentSessionLifetimeUnlimited = false,
                LogoutConsent = LoginUpPartyLogoutConsent.Never
            };
            await mLoginUpParty.SetIdAsync(new Party.IdKey { TenantName = tenantName?.ToLower(), TrackName = Constants.Routes.MasterTrackName, PartyName = loginName });

            await tenantService.CreateAsync(mLoginUpParty);
            return mLoginUpParty;
        }

        public async Task CreateFirstAdminUserDocumentAsync(string tenantName, string email, string password)
        {
            //var claims = new List<ClaimAndValues> { new ClaimAndValues { Claim = JwtClaimTypes.Role, Values = adminUserRoles.ToList() } };
            await accountLogic.CreateUser(email, password, tenantName: tenantName?.ToLower(), trackName: Constants.Routes.MasterTrackName, checkUserAndPasswordPolicy: false);
        }

        public async Task CreateFoxIDsControlApiResourceDocumentAsync(string tenantName, bool includeMasterTenantScope = false)
        {
            var mControlApiResourceDownParty = new OAuthDownParty
            {
                Name = controlApiResourceName
            };
            await mControlApiResourceDownParty.SetIdAsync(new Party.IdKey { TenantName = tenantName?.ToLower(), TrackName = Constants.Routes.MasterTrackName, PartyName = controlApiResourceName });
           
            var scopes = new List<string> { controlApiResourceTenantScope };
            if (includeMasterTenantScope)
            {
                scopes.Add(controlApiResourceMasterScope);
            }
            mControlApiResourceDownParty.Resource = new OAuthDownResource()
            {
                Scopes = scopes
            };

            await tenantService.CreateAsync(mControlApiResourceDownParty);
        }

        public async Task CreateControlClientDocmentAsync(string tenantName, string controlClientBaseUri, LoginUpParty loginUpParty, bool includeMasterTenantScope = false)
        {
            var mControlClientDownParty = new OidcDownParty
            {
                Name = controlClientName
            };
            await mControlClientDownParty.SetIdAsync(new Party.IdKey { TenantName = tenantName?.ToLower(), TrackName = Constants.Routes.MasterTrackName, PartyName = controlClientName });
            mControlClientDownParty.AllowUpParties = new List<UpPartyLink> { new UpPartyLink { Name = loginUpParty.Name?.ToLower(), Type = loginUpParty.Type } };
            mControlClientDownParty.AllowCorsOrigins = GetControlClientAllowCorsOrigins(controlClientBaseUri);

            var scopes = new List<string> { controlApiResourceTenantScope };
            if (includeMasterTenantScope)
            {
                scopes.Add(controlApiResourceMasterScope);
            }
            mControlClientDownParty.Client = new OidcDownClient
            {
                RedirectUris = GetControlClientRedirectUris(tenantName?.ToLower(), controlClientBaseUri).ToList(),
                ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = controlApiResourceName, Scopes = scopes } },
                ResponseTypes = new[] { "code" }.ToList(),
                Scopes = GetControlClientScopes(),
                EnablePkce = true,
                AuthorizationCodeLifetime = 10,
                IdTokenLifetime = 1800, // 30 minutes
                AccessTokenLifetime = 1800, // 30 minutes
                RefreshTokenLifetime = 86400, // 24 hours
                RefreshTokenAbsoluteLifetime = 86400, // 24 hours
                RefreshTokenUseOneTime = true,
                RefreshTokenLifetimeUnlimited = false,
                RequireLogoutIdTokenHint = true,
            };
            
            await tenantService.CreateAsync(mControlClientDownParty);
        }

        private List<string> GetControlClientAllowCorsOrigins(string controlClientBaseUri)
        {
            return new List<string> { controlClientBaseUri.TrimEnd('/') };
        }

        private List<OidcDownScope> GetControlClientScopes()
        {
            return new List<OidcDownScope>
            {
                new OidcDownScope { Scope = "offline_access" },
                new OidcDownScope { Scope = "profile", VoluntaryClaims = new List<OidcDownClaim>
                    {
                        new OidcDownClaim { Claim = "name", InIdToken = true  },
                        new OidcDownClaim { Claim = "family_name", InIdToken = true  },
                        new OidcDownClaim { Claim = "given_name", InIdToken = true  },
                        new OidcDownClaim { Claim = "middle_name", InIdToken = true  },
                        new OidcDownClaim { Claim = "nickname" },
                        new OidcDownClaim { Claim = "preferred_username" },
                        new OidcDownClaim { Claim = "profile" },
                        new OidcDownClaim { Claim = "picture" },
                        new OidcDownClaim { Claim = "website" },
                        new OidcDownClaim { Claim = "gender" },
                        new OidcDownClaim { Claim = "birthdate" },
                        new OidcDownClaim { Claim = "zoneinfo" },
                        new OidcDownClaim { Claim = "locale" },
                        new OidcDownClaim { Claim = "updated_at" }
                    }
                },
                new OidcDownScope { Scope = "email", VoluntaryClaims = new List<OidcDownClaim>
                    {
                        new OidcDownClaim { Claim = "email", InIdToken = true  },
                        new OidcDownClaim { Claim = "email_verified" }
                    }
                },
            };
        }

        private IEnumerable<string> GetControlClientRedirectUris(string tenantName, string baseUrl)
        {
            yield return UrlCombine.Combine(baseUrl, tenantName, "authentication/login_callback");
            yield return UrlCombine.Combine(baseUrl, tenantName, "authentication/logout_callback");
        }
    }
}
