using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ITfoxtec.Identity.Util;
using static ITfoxtec.Identity.IdentityConstants;
using FoxIDs.Util;
using FoxIDs.Models.Logic;

namespace FoxIDs.Logic
{
    public class MasterTenantLogic : LogicBase
    {
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly BaseAccountLogic accountLogic;
        private readonly TrackLogic trackLogic;

        public MasterTenantLogic(ITenantDataRepository tenantDataRepository, BaseAccountLogic accountLogic, TrackLogic trackLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tenantDataRepository = tenantDataRepository;
            this.accountLogic = accountLogic;
            this.trackLogic = trackLogic;
        }

        public async Task CreateMasterTrackDocumentAsync(string tenantName)
        {
            tenantName = tenantName?.ToLower();
            var trackName = Constants.Routes.MasterTrackName;

            var mTrack = new Track
            {
                DisplayName = "Master",
                Name = trackName,
                SequenceLifetime = 1800,
                AutoMapSamlClaims = true,
                MaxFailingLogins = 5,
                FailingLoginCountLifetime = 36000,
                FailingLoginObservationPeriod = 600,
                PasswordLength = 8,
                PasswordMaxLength = Constants.Models.Track.PasswordLengthMax,
                CheckPasswordComplexity = true,
                CheckPasswordRisk = true,
                PasswordHistory = 0,
                PasswordMaxAge = 0,
                SoftPasswordChange = 0
            };
            await mTrack.SetIdAsync(new Track.IdKey { TenantName = tenantName, TrackName = trackName });

            await trackLogic.CreateTrackDocumentAsync(mTrack, tenantName);
        }

        public async Task CreateTrackDocumentAsync(string tenantName, Track mTrack)
        {
            tenantName = tenantName?.ToLower();

            await mTrack.SetIdAsync(new Track.IdKey { TenantName = tenantName, TrackName = mTrack.Name });

            await trackLogic.CreateTrackDocumentAsync(mTrack, tenantName);
        }

        public async Task<LoginUpParty> CreateMasterLoginDocumentAsync(string tenantName)
        {
            var mLoginUpParty = new LoginUpParty
            {
                DisplayName = "Default",
                Name = Constants.DefaultLogin.Name,
                EnableCreateUser = false,
                EnableCancelLogin = false,
                DeleteRefreshTokenGrantsOnChangePassword = true,
                DeleteActiveSessionOnChangePassword = true,
                SessionLifetime = 36000, // 10 hours
                SessionAbsoluteLifetime = 36000, // 10 hours
                PersistentSessionLifetimeUnlimited = false,
                LogoutConsent = LoginUpPartyLogoutConsents.IfRequired
            };
            var partyIdKey = new Party.IdKey { TenantName = tenantName?.ToLower(), TrackName = Constants.Routes.MasterTrackName, PartyName = Constants.DefaultLogin.Name };
            await mLoginUpParty.SetIdAsync(partyIdKey);

            await tenantDataRepository.CreateAsync(mLoginUpParty);

            return mLoginUpParty;
        }

        public async Task<LoginUpParty> CreateLoginDocumentAsync(string tenantName, string trackName)
        {
            var mLoginUpParty = new LoginUpParty
            {
                DisplayName = "Default",
                Name = Constants.DefaultLogin.Name,
                EnableCreateUser = true,                
                EnableCancelLogin = false,
                SessionLifetime = 36000, // 10 hours
                PersistentSessionLifetimeUnlimited = false,
                LogoutConsent = LoginUpPartyLogoutConsents.IfRequired
            };
            var partyIdKey = new Party.IdKey { TenantName = tenantName?.ToLower(), TrackName = trackName, PartyName = Constants.DefaultLogin.Name };
            await mLoginUpParty.SetIdAsync(partyIdKey);

            await tenantDataRepository.CreateAsync(mLoginUpParty);

            return mLoginUpParty;
        }

        public async Task CreateFirstAdminUserDocumentAsync(string tenantName, string email, string password, bool changePassword, bool checkUserAndPasswordPolicy, bool confirmAccount, bool setPasswordEmail = false, bool isMasterTenant = false)
        {
            var claims = new List<Claim> { new Claim(JwtClaimTypes.Role, isMasterTenant ? Constants.ControlApi.Access.TenantAdminRole : Constants.ControlApi.Access.Tenant) };
            await accountLogic.CreateUserAsync(new CreateUserObj 
            {
                UserIdentifier = new UserIdentifier { Email = email },
                Password = password,
                ChangePassword = changePassword,
                SetPasswordEmail = setPasswordEmail,
                Claims = claims, 
                ConfirmAccount = confirmAccount 
            }, checkUserAndPasswordPolicy: checkUserAndPasswordPolicy, tenantName: tenantName?.ToLower(), trackName: Constants.Routes.MasterTrackName);
        }

        public async Task CreateMasterFoxIDsControlApiResourceDocumentAsync(string tenantName, bool isMasterTenant = false)
        {
            var mControlApiResourceDownParty = new OAuthDownParty
            {
                DisplayName = "FoxIDs Control API",
                Name = Constants.ControlApi.ResourceName
            };
            var partyIdKey = new Party.IdKey { TenantName = tenantName?.ToLower(), TrackName = Constants.Routes.MasterTrackName, PartyName = Constants.ControlApi.ResourceName };
            await mControlApiResourceDownParty.SetIdAsync(partyIdKey);
           
            var scopes = new List<string>
            {
                Constants.ControlApi.Access.Tenant,
                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Read}",

                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.Segment.Basic}",
                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.Segment.Basic}{Constants.ControlApi.AccessElement.Read}",


                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]",
                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]{Constants.ControlApi.AccessElement.Read}",

                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]{Constants.ControlApi.Segment.Usage}",

                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]{Constants.ControlApi.Segment.Log}",
                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]{Constants.ControlApi.Segment.Log}{Constants.ControlApi.AccessElement.Read}",

                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]{Constants.ControlApi.Segment.User}",
                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]{Constants.ControlApi.Segment.User}{Constants.ControlApi.AccessElement.Read}",

                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]{Constants.ControlApi.Segment.Party}",
                $"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}[{Constants.Routes.MasterTrackName}]{Constants.ControlApi.Segment.Party}{Constants.ControlApi.AccessElement.Read}"
            };

            if (!isMasterTenant)
            {
                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}");
                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}{Constants.ControlApi.AccessElement.Read}");

                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}{Constants.ControlApi.Segment.Usage}");

                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}{Constants.ControlApi.Segment.Log}");
                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}{Constants.ControlApi.Segment.Log}{Constants.ControlApi.AccessElement.Read}");

                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}{Constants.ControlApi.Segment.User}");
                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}{Constants.ControlApi.Segment.User}{Constants.ControlApi.AccessElement.Read}");

                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}{Constants.ControlApi.Segment.Party}");
                scopes.Add($"{Constants.ControlApi.Access.Tenant}{Constants.ControlApi.AccessElement.Track}{Constants.ControlApi.Segment.Party}{Constants.ControlApi.AccessElement.Read}");
            }
            else
            {
                scopes.Add(Constants.ControlApi.Access.Master);
                scopes.Add($"{Constants.ControlApi.Access.Master}{Constants.ControlApi.AccessElement.Read}");
                scopes.Add($"{Constants.ControlApi.Access.Master}{Constants.ControlApi.Segment.Usage}");
            }
            mControlApiResourceDownParty.Resource = new OAuthDownResource()
            {
                Scopes = scopes
            };

            await tenantDataRepository.CreateAsync(mControlApiResourceDownParty);
        }

        public async Task CreateMasterControlClientDocumentAsync(string tenantName, string controlClientBaseUri, LoginUpParty loginUpParty, bool includeMasterTenantScope = false)
        {
            var mControlClientDownParty = new OidcDownParty
            {
                DisplayName = "FoxIDs Control Client",
                Name = Constants.ControlClient.ClientId
            };
            var partyIdKey = new Party.IdKey { TenantName = tenantName?.ToLower(), TrackName = Constants.Routes.MasterTrackName, PartyName = Constants.ControlClient.ClientId };
            await mControlClientDownParty.SetIdAsync(partyIdKey);
            mControlClientDownParty.AllowUpParties = new List<UpPartyLink> { new UpPartyLink { Name = loginUpParty.Name?.ToLower(), Type = loginUpParty.Type } };
            mControlClientDownParty.AllowCorsOrigins = new List<string> { controlClientBaseUri.UrlToOrigin() };

            var scopes = new List<string> { Constants.ControlApi.Access.Tenant };
            if (includeMasterTenantScope)
            {
                scopes.Add(Constants.ControlApi.Access.Master);
            }
            mControlClientDownParty.Client = new OidcDownClient
            {
                RedirectUris = GetControlClientRedirectUris(tenantName?.ToLower(), controlClientBaseUri).ToList(),
                ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = Constants.ControlApi.ResourceName, Scopes = scopes } },
                ResponseTypes = new[] { "code" }.ToList(),
                Scopes = GetControlClientScopes(),
                Claims = GetControlClientClaims(),
                RequirePkce = true,
                AuthorizationCodeLifetime = 30,
                IdTokenLifetime = 3600, // 1 hours
                AccessTokenLifetime = 3600, // 1 hours
                RefreshTokenLifetime = 7200, // 2 hours
                RefreshTokenAbsoluteLifetime = 21600, // 6 hours
                RefreshTokenUseOneTime = true,
                RefreshTokenLifetimeUnlimited = false,
                RequireLogoutIdTokenHint = false,
                DisableClientCredentialsGrant = true
            };
            
            await tenantDataRepository.CreateAsync(mControlClientDownParty);
        }

        private List<OidcDownScope> GetControlClientScopes()
        {
            return new List<OidcDownScope>
            {
                new OidcDownScope { Scope = DefaultOidcScopes.OfflineAccess },
                new OidcDownScope { Scope = DefaultOidcScopes.Profile, VoluntaryClaims = new List<OidcDownClaim>
                    {
                        new OidcDownClaim { Claim = JwtClaimTypes.Name, InIdToken = true  },
                        new OidcDownClaim { Claim = JwtClaimTypes.FamilyName, InIdToken = true  },
                        new OidcDownClaim { Claim = JwtClaimTypes.GivenName, InIdToken = true  },
                        new OidcDownClaim { Claim = JwtClaimTypes.MiddleName, InIdToken = true  },
                        new OidcDownClaim { Claim = JwtClaimTypes.Locale }
                    }
                },
                new OidcDownScope { Scope = DefaultOidcScopes.Email, VoluntaryClaims = new List<OidcDownClaim>
                    {
                        new OidcDownClaim { Claim = JwtClaimTypes.Email, InIdToken = true  },
                        new OidcDownClaim { Claim = JwtClaimTypes.EmailVerified }
                    }
                },
            };
        }

        private List<OidcDownClaim> GetControlClientClaims()
        {
            return new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Role, InIdToken = true } };
        }

        private IEnumerable<string> GetControlClientRedirectUris(string tenantName, string baseUrl)
        {
            yield return UrlCombine.Combine(baseUrl, tenantName, "authentication/login_callback");
            yield return UrlCombine.Combine(baseUrl, tenantName, "authentication/logout_callback");
        }

        public async Task CreateDefaultTracksDocmentsAsync(string tenantName)
        {
            var testTrackName = await CreateTrackDocumentsAsync(tenantName, Constants.TrackDefaults.DefaultTrackTestDisplayName, RandomName.GenerateDefaultName());
            var prodTrackName = await CreateTrackDocumentsAsync(tenantName, Constants.TrackDefaults.DefaultTrackProductionDisplayName, Constants.TrackDefaults.DefaultTrackProductionName);
        }

        private async Task<string> CreateTrackDocumentsAsync(string tenantName, string trackDisplayName, string trackName)
        {
            var mTrack = new Track
            {
                DisplayName = trackDisplayName,
                Name = trackName?.ToLower(), 
                AutoMapSamlClaims = true,
            };
            await CreateTrackDocumentAsync(tenantName, mTrack);
            await CreateLoginDocumentAsync(tenantName, mTrack.Name);
            return mTrack.Name;
        }
    }
}
