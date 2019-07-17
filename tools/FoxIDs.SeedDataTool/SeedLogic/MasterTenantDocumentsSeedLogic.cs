using FoxIDs;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.SeedDataTool.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FoxIDs.SeedDataTool.SeedLogic
{
    public class MasterTenantDocumentsSeedLogic
    {
        const string masterTenantName = "master";
        const string masterTrackName = "_";
        const string loginName = "login";
        const string apiResourceName = "foxids_api";
        const string seedClientName = "foxids_seed";
        const string portalClientName = "foxids_portal";

        readonly string[] apiResourceScopes = new[] { "master", "tenant" };
        readonly string[] adminUserClaims = new[] { "master_admin" };
        readonly string[] seedClientRedirectUris = new[] { "uri:seed:client" };
 
        readonly long timeStamp;

        private readonly SecretHashLogic secretHashLogic;
        private readonly SimpleTenantRepository simpleTenantRepository;
        private static readonly JsonSerializerSettings SettingsIndented = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            Formatting = Formatting.Indented
        };

        public MasterTenantDocumentsSeedLogic(SecretHashLogic secretHashLogic, SimpleTenantRepository simpleTenantRepository)
        {
            this.secretHashLogic = secretHashLogic;
            this.simpleTenantRepository = simpleTenantRepository;

            timeStamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public async Task SeedAsync()
        {
            Console.WriteLine("Creating master tenant documents");

            await CreateDocumentsAsync();

            Console.WriteLine(string.Empty);
            Console.WriteLine($"Master tenant documents created and saved in Cosmos DB");
        }

        private async Task CreateDocumentsAsync()
        {
            await CreateMasterTenantDocumentAsync();
            await CreateMasterTrackDocumentAsync();
            var loginUpParty = await CreateLoginDocumentAsync();
            Console.WriteLine(string.Empty);

            await CreateFirstAdminUserDocumentAsync();
            Console.WriteLine(string.Empty);

            await CreateApiResourceDocumentAsync();
            Console.WriteLine(string.Empty);
            await CreateSeedClientDocmentAsync();
            Console.WriteLine(string.Empty);
            await CreatePortalClientDocmentAsync(loginUpParty);
            Console.WriteLine(string.Empty);
        }

        private async Task CreateMasterTenantDocumentAsync()
        {
            Console.WriteLine("Creating tenant");

            var masterTenant = new Tenant();
            await masterTenant.SetIdAsync(new Tenant.IdKey { TenantName = masterTenantName });
            masterTenant.SetPartitionId();

            await simpleTenantRepository.SaveAsync(masterTenant);
            Console.WriteLine("Tenant document created and saved in Cosmos DB");
        }

        private async Task CreateMasterTrackDocumentAsync()
        {
            Console.WriteLine("Creating track");

            var masterTrack = new Track
            {
                SequenceLifetime = 30,
                PasswordLength = 8,
                CheckPasswordComplexity = true,
                CheckPasswordRisk = true
            };
            await masterTrack.SetIdAsync(new Track.IdKey { TenantName = masterTenantName, TrackName = masterTrackName });
            masterTrack.SetPartitionId();
            masterTrack.PrimaryKey = await CreateX509KeyAsync();

            await simpleTenantRepository.SaveAsync(masterTrack);
            Console.WriteLine($"Track document created and saved in Cosmos DB");
        }

        private async Task<TrackKey> CreateX509KeyAsync()
        {
            var certificate = await masterTenantName.CreateSelfSignedCertificateAsync();

            var trackKey = new TrackKey()
            {
                Type = TrackKeyType.Contained.ToString(),
                Key = await certificate.ToJsonWebKeyAsync(true)
            };
            return trackKey;
        }

        private async Task<LoginUpParty> CreateLoginDocumentAsync()
        {
            Console.WriteLine("Creating login");

            var loginUpParty = new LoginUpParty
            {
                Type = PartyType.Login.ToString(),
                EnableCreateUser = true,
                EnableCancelLogin = false,
                SessionLifetime = 0,
                PersistentSessionLifetimeUnlimited = false,
                LogoutConsent = LoginUpPartyLogoutConsent.Never.ToString()
            };
            await loginUpParty.SetIdAsync(new Party.IdKey { TenantName = masterTenantName, TrackName = masterTrackName, PartyName = loginName });
            loginUpParty.SetPartitionId();

            await simpleTenantRepository.SaveAsync(loginUpParty);
            Console.WriteLine($"Login document created and saved in Cosmos DB");

            return loginUpParty;
        }

        private async Task CreateFirstAdminUserDocumentAsync()
        {
            Console.WriteLine("Creating first administrator user");
            Console.Write("Please enter the administrator user email: ");
            var email = Console.ReadLine();
            if (!new EmailAddressAttribute().IsValid(email))
            {
                throw new Exception($"Email '{email}' is invalid.");
            }
            var password = RandomGenerator.Generate(16);
            Console.WriteLine($"Administrator users password is: {password}");

            var user = new User { UserId = Guid.NewGuid().ToString() };
            await user.SetIdAsync(new User.IdKey { TenantName = masterTenantName, TrackName = masterTrackName, Email = email });
            await secretHashLogic.AddSecretHashAsync(user, password);
            user.Claims = new List<ClaimAndValues> { new ClaimAndValues { Claim = JwtClaimTypes.Role, Values = adminUserClaims.ToList() } };
            user.SetPartitionId();

            await simpleTenantRepository.SaveAsync(user);
            Console.WriteLine($"Administrator user document created and saved in Cosmos DB");
        }

        private async Task CreateApiResourceDocumentAsync()
        {
            Console.WriteLine("Creating api resource");

            var apiResourceDownParty = new OAuthDownParty();
            await apiResourceDownParty.SetIdAsync(new Party.IdKey { TenantName = masterTenantName, TrackName = masterTrackName, PartyName = apiResourceName });
            apiResourceDownParty.Resource = new OAuthDownResource
            {
                Scopes = apiResourceScopes.ToList()
            };
            apiResourceDownParty.SetPartitionId();

            await simpleTenantRepository.SaveAsync(apiResourceDownParty);
            Console.WriteLine($"Api resource document created and saved in Cosmos DB");
        }

        private async Task CreateSeedClientDocmentAsync()
        {
            Console.WriteLine("Creating seed client");

            var seedClientDownParty = new OAuthDownParty();
            await seedClientDownParty.SetIdAsync(new Party.IdKey { TenantName = masterTenantName, TrackName = masterTrackName, PartyName = seedClientName });
            seedClientDownParty.Client = new OAuthDownClient
            {
                RedirectUris = seedClientRedirectUris.ToList(),
                ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = apiResourceName, Scopes = new[] { "master" }.ToList() } },
                ResponseTypes = new[] { "token" }.ToList(),
                AccessTokenLifetime = 1800 // 30 minutes
            };

            (var secret, var oauthClientSecret) = await CreateSecretAsync();
            seedClientDownParty.Client.Secrets = new List<OAuthClientSecret> { oauthClientSecret };
            seedClientDownParty.SetPartitionId();

            await simpleTenantRepository.SaveAsync(seedClientDownParty);
            Console.WriteLine("Seed client document created and saved in Cosmos DB");
            Console.WriteLine($"Seed client secret is: {secret}");
        }

        private async Task CreatePortalClientDocmentAsync(LoginUpParty loginUpParty)
        {
            Console.WriteLine("Creating portal client");
            Console.Write("Please enter portal domain: ");
            var portalDomain = Console.ReadLine();
            Console.Write("Please click 'T' to add the localhost test domain otherwise click another key: ");
            var addLocalhostDomain = Console.ReadKey();
            Console.WriteLine(string.Empty);

            var portalClientRedirectUris = new List<string>();
            portalClientRedirectUris.Add($"https://{portalDomain}/authresponse");
            if(char.ToLower(addLocalhostDomain.KeyChar) == 't')
            {
                portalClientRedirectUris.Add("https://localhost:44332");
            }

            var portalClientDownParty = new OidcDownParty();
            await portalClientDownParty.SetIdAsync(new Party.IdKey { TenantName = masterTenantName, TrackName = masterTrackName, PartyName = portalClientName });
            portalClientDownParty.AllowUpParties = new List<PartyDataElement> { new PartyDataElement { Id = loginUpParty.Id, Type = loginUpParty.Type } };
            portalClientDownParty.Client = new OidcDownClient
            {
                RedirectUris = portalClientRedirectUris.ToList(),
                ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = apiResourceName, Scopes = new[] { "tenant" }.ToList() } },
                ResponseTypes = new[] { "code", "id_token" }.ToList(),
                AuthorizationCodeLifetime = 10,
                IdTokenLifetime = 1800, // 30 minutes
                AccessTokenLifetime = 1800, // 30 minutes
                RefreshTokenLifetime = 86400, // 24 hours
                RefreshTokenAbsoluteLifetime = 86400, // 24 hours
                RefreshTokenUseOneTime = true,
                RefreshTokenLifetimeUnlimited = false,
                RequireLogoutIdTokenHint = true,
            };

            (var secret, var oauthClientSecret) = await CreateSecretAsync();
            portalClientDownParty.Client.Secrets = new List<OAuthClientSecret> { oauthClientSecret };
            portalClientDownParty.SetPartitionId();

            await simpleTenantRepository.SaveAsync(portalClientDownParty);
            Console.WriteLine("Portal client document created and saved in Cosmos DB");
            Console.WriteLine($"Portal client secret is: {secret}");
        }

        private async Task<(string, OAuthClientSecret)> CreateSecretAsync()
        {
            var oauthClientSecret = new OAuthClientSecret();
            var secret = RandomGenerator.Generate(32);
            await secretHashLogic.AddSecretHashAsync(oauthClientSecret, secret);
            return (secret, oauthClientSecret);
        }

        public string ToJsonIndented(object obj)
        {
            return JsonConvert.SerializeObject(obj, SettingsIndented);
        }
    }
}
