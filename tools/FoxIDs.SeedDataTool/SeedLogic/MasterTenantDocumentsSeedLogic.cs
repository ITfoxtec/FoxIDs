using FoxIDs;
using FoxIDs.Logic;
using FoxIDs.Models;
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

        string masterTenantDocumentName => $"tenant_master_{timeStamp}.json";
        string masterTrackDocumentName => $"track_master_{timeStamp}.json";
        string loginDocumentName => $"up_party_login_{timeStamp}.json";
        string firstAdminUserDocumentName => $"first_admin_user_{timeStamp}.json";
        string apiResourceDocumentName => $"down_party_api_resource_{timeStamp}.json";
        string seedClientDocumentName => $"down_party_seed_client_{timeStamp}.json";
        string portalClientDocumentName => $"down_party_portal_client_{timeStamp}.json";

        private readonly SecretHashLogic secretHashLogic;

        private static readonly JsonSerializerSettings SettingsIndented = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            Formatting = Formatting.Indented
        };

        public MasterTenantDocumentsSeedLogic(SecretHashLogic secretHashLogic)
        {
            this.secretHashLogic = secretHashLogic;

            timeStamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public async Task SeedAsync()
        {
            var appLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var masterTenantPath = $"{appLocation}\\marster_tenant";
            Directory.CreateDirectory(masterTenantPath);

            await CreateDocumentsAsync(masterTenantPath);

            Console.WriteLine($"Master tenant documents was created {masterTenantPath}");
        }

        private async Task CreateDocumentsAsync(string masterTenantPath)
        {
            await CreateMasterTenantDocumentAsync(masterTenantPath);
            await CreateMasterTrackDocumentAsync(masterTenantPath);

            var loginUpParty = await CreateLoginDocumentAsync(masterTenantPath);
            await CreateFirstAdminUserDocumentAsync(masterTenantPath);

            await CreateApiResourceDocumentAsync(masterTenantPath);
            await CreateSeedClientDocmentAsync(masterTenantPath);
            await CreatePortalClientDocmentAsync(masterTenantPath, loginUpParty);
        }

        private async Task CreateMasterTenantDocumentAsync(string masterTenantPath)
        {
            Console.WriteLine("Creating tenant.");

            var masterTenant = new Tenant();
            await masterTenant.SetIdAsync(new Tenant.IdKey { TenantName = masterTenantName });
            masterTenant.SetPartitionId();

            await masterTenant.ValidateObjectAsync();

            File.WriteAllText(Path.Combine(masterTenantPath, masterTenantDocumentName), ToJsonIndented(masterTenant));
            Console.WriteLine($"{masterTenantDocumentName} created");
        }

        private async Task CreateMasterTrackDocumentAsync(string masterTenantPath)
        {
            Console.WriteLine("Creating track.");

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

            await masterTrack.ValidateObjectAsync();

            File.WriteAllText(Path.Combine(masterTenantPath, masterTrackDocumentName), ToJsonIndented(masterTrack));
            Console.WriteLine($"{masterTrackDocumentName} created");
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

        private async Task<LoginUpParty> CreateLoginDocumentAsync(string masterTenantPath)
        {
            Console.WriteLine("Creating login.");

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

            await loginUpParty.ValidateObjectAsync();

            File.WriteAllText(Path.Combine(masterTenantPath, loginDocumentName), ToJsonIndented(loginUpParty));
            Console.WriteLine($"{loginDocumentName} created");

            return loginUpParty;
        }

        private async Task CreateFirstAdminUserDocumentAsync(string masterTenantPath)
        {
            Console.WriteLine("Creating first administrator user.");
            Console.Write("Please enter email: ");
            var email = Console.ReadLine();
            if (!new EmailAddressAttribute().IsValid(email))
            {
                throw new Exception($"Email '{email}' is invalid.");
            }
            var password = RandomGenerator.Generate(16);
            Console.WriteLine($"Password: {password}");

            var user = new User { UserId = Guid.NewGuid().ToString() };
            await user.SetIdAsync(new User.IdKey { TenantName = masterTenantName, TrackName = masterTrackName, Email = email });
            await secretHashLogic.AddSecretHashAsync(user, password);
            user.Claims = new List<ClaimAndValues> { new ClaimAndValues { Claim = JwtClaimTypes.Role, Values = adminUserClaims.ToList() } };
            user.SetPartitionId();

            await user.ValidateObjectAsync();

            File.WriteAllText(Path.Combine(masterTenantPath, firstAdminUserDocumentName), ToJsonIndented(user));
            Console.WriteLine($"{firstAdminUserDocumentName} created");
        }

        private async Task CreateApiResourceDocumentAsync(string masterTenantPath)
        {
            Console.WriteLine("Creating api resource.");

            var apiResourceDownParty = new OAuthDownParty();
            await apiResourceDownParty.SetIdAsync(new Party.IdKey { TenantName = masterTenantName, TrackName = masterTrackName, PartyName = apiResourceName });
            apiResourceDownParty.Resource = new OAuthDownResource
            {
                Scopes = apiResourceScopes.ToList()
            };
            apiResourceDownParty.SetPartitionId();

            await apiResourceDownParty.ValidateObjectAsync();

            File.WriteAllText(Path.Combine(masterTenantPath, apiResourceDocumentName), ToJsonIndented(apiResourceDownParty));
            Console.WriteLine($"{apiResourceDocumentName} created");
        }

        private async Task CreateSeedClientDocmentAsync(string masterTenantPath)
        {
            Console.WriteLine("Creating seed client.");

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

            await seedClientDownParty.ValidateObjectAsync();

            File.WriteAllText(Path.Combine(masterTenantPath, seedClientDocumentName), ToJsonIndented(seedClientDownParty));
            Console.WriteLine($"{seedClientDocumentName} created with secret {secret}");
        }

        private async Task CreatePortalClientDocmentAsync(string masterTenantPath, LoginUpParty loginUpParty)
        {
            Console.WriteLine("Creating portal client.");
            Console.Write("Please enter portal domain: ");
            var portalDomain = Console.ReadLine();
            Console.Write("Please click 'T' to add the localhost test domain otherwise not added: ");
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

            await portalClientDownParty.ValidateObjectAsync();

            File.WriteAllText(Path.Combine(masterTenantPath, portalClientDocumentName), ToJsonIndented(portalClientDownParty));
            Console.WriteLine($"{portalClientDocumentName} created with secret {secret}");
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
