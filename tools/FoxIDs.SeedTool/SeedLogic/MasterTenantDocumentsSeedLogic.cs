using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.SeedTool.Model;
using FoxIDs.SeedTool.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.SeedTool.SeedLogic
{
    public class MasterTenantDocumentsSeedLogic
    {
        const string loginName = "login";
        const string apiResourceName = "foxids_api";
        const string controlClientName = "foxids_control";

        const string apiResourceMasterScope = "foxids_master";
        const string apiResourceTenantScope = "foxids_tenant";
        static readonly string[] apiResourceScopes = new[] { apiResourceMasterScope, apiResourceTenantScope };
        static readonly string[] adminUserRoles = new[] { "foxids_master_admin" };
 
        private readonly SeedSettings settings;
        private readonly SecretHashLogic secretHashLogic;
        private readonly SimpleTenantRepository simpleTenantRepository;

        public MasterTenantDocumentsSeedLogic(SeedSettings settings, SecretHashLogic secretHashLogic, SimpleTenantRepository simpleTenantRepository)
        {
            this.settings = settings;
            this.secretHashLogic = secretHashLogic;
            this.simpleTenantRepository = simpleTenantRepository;
        }

        public async Task SeedAsync()
        {
            try
            {
                await settings.CosmosDb.ValidateObjectAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidConfigException("The Cosmos DB configuration is required to create the master tenant documents.", ex);
            }

            Console.WriteLine("Creating master tenant documents");

            await simpleTenantRepository.InitiateAsync();
            await CreateDocumentsAsync();

            Console.WriteLine(string.Empty);
            Console.WriteLine("Important: remember the password and secrets");
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

            await CreateFoxIDsApiResourceDocumentAsync();
            Console.WriteLine(string.Empty);
            await CreateControlClientDocmentAsync(loginUpParty);
            Console.WriteLine(string.Empty);
            await CreateSeedClientDocmentAsync();
            Console.WriteLine(string.Empty);
        }

        private async Task CreateMasterTenantDocumentAsync()
        {
            Console.WriteLine("Creating tenant");

            var masterTenant = new Tenant();
            await masterTenant.SetIdAsync(new Tenant.IdKey { TenantName = settings.MasterTenant });
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
            await masterTrack.SetIdAsync(new Track.IdKey { TenantName = settings.MasterTenant, TrackName = settings.MasterTrack });
            masterTrack.SetPartitionId();
            masterTrack.PrimaryKey = await CreateX509KeyAsync();

            await simpleTenantRepository.SaveAsync(masterTrack);
            Console.WriteLine($"Track document created and saved in Cosmos DB");
        }

        private async Task<TrackKey> CreateX509KeyAsync()
        {
            var certificate = await settings.MasterTenant.CreateSelfSignedCertificateAsync();

            var trackKey = new TrackKey()
            {
                Type = TrackKeyType.Contained,
                Key = await certificate.ToJsonWebKeyAsync(true)
            };
            return trackKey;
        }

        private async Task<LoginUpParty> CreateLoginDocumentAsync()
        {
            Console.WriteLine("Creating login");

            var loginUpParty = new LoginUpParty
            {
                EnableCreateUser = true,
                EnableCancelLogin = false,
                SessionLifetime = 0,
                PersistentSessionLifetimeUnlimited = false,
                LogoutConsent = LoginUpPartyLogoutConsent.Never
            };
            await loginUpParty.SetIdAsync(new Party.IdKey { TenantName = settings.MasterTenant, TrackName = settings.MasterTrack, PartyName = loginName });
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
            await user.SetIdAsync(new User.IdKey { TenantName = settings.MasterTenant, TrackName = settings.MasterTrack, Email = email });
            await secretHashLogic.AddSecretHashAsync(user, password);
            user.Claims = new List<ClaimAndValues> { new ClaimAndValues { Claim = JwtClaimTypes.Role, Values = adminUserRoles.ToList() } };
            user.SetPartitionId();

            await simpleTenantRepository.SaveAsync(user);
            Console.WriteLine($"Administrator user document created and saved in Cosmos DB");
        }

        private async Task CreateFoxIDsApiResourceDocumentAsync()
        {
            Console.WriteLine("Creating FoxIDs api resource");

            var apiResourceDownParty = new OAuthDownParty();
            await apiResourceDownParty.SetIdAsync(new Party.IdKey { TenantName = settings.MasterTenant, TrackName = settings.MasterTrack, PartyName = apiResourceName });
            apiResourceDownParty.Resource = new OAuthDownResource
            {
                Scopes = apiResourceScopes.ToList()
            };
            apiResourceDownParty.SetPartitionId();

            await simpleTenantRepository.SaveAsync(apiResourceDownParty);
            Console.WriteLine($"FoxIDs api resource document created and saved in Cosmos DB");
        }

        private async Task CreateControlClientDocmentAsync(LoginUpParty loginUpParty)
        {
            Console.WriteLine("Creating control client");
            Console.Write("Add localhost test domain to enable local development [y/n] (default no): ");
            var addLocalhostDomain = Console.ReadKey();
            Console.WriteLine(string.Empty);

            var controlClientAllowCorsOrigins = new List<string>();
            controlClientAllowCorsOrigins.Add(settings.FoxIDsControlAuthResponseEndpoint);
            if (char.ToLower(addLocalhostDomain.KeyChar) == 'y')
            {
                controlClientAllowCorsOrigins.Add("https://localhost:44332");
            }

            var controlClientRedirectUris = new List<string>();
            controlClientRedirectUris.Add(UrlCombine.Combine(settings.FoxIDsControlAuthResponseEndpoint, settings.MasterTenant));
            if (char.ToLower(addLocalhostDomain.KeyChar) == 'y')
            {
                controlClientRedirectUris.Add(UrlCombine.Combine("https://localhost:44332", settings.MasterTenant));
            }

            var controlClientDownParty = new OidcDownParty();
            await controlClientDownParty.SetIdAsync(new Party.IdKey { TenantName = settings.MasterTenant, TrackName = settings.MasterTrack, PartyName = controlClientName });
            controlClientDownParty.AllowUpParties = new List<UpPartyLink> { new UpPartyLink { Name = loginUpParty.Name, Type = loginUpParty.Type } };
            controlClientDownParty.AllowCorsOrigins = controlClientAllowCorsOrigins;
            controlClientDownParty.Client = new OidcDownClient
            {
                RedirectUris = controlClientRedirectUris,
                ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = apiResourceName, Scopes = new[] { apiResourceTenantScope }.ToList() } },
                ResponseTypes = new[] { "code" }.ToList(),
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
            controlClientDownParty.SetPartitionId();

            await simpleTenantRepository.SaveAsync(controlClientDownParty);
            Console.WriteLine("Control client document created and saved in Cosmos DB");
        }

        private async Task CreateSeedClientDocmentAsync()
        {
            Console.WriteLine("Creating seed client");

            var seedClientDownParty = new OAuthDownParty();
            await seedClientDownParty.SetIdAsync(new Party.IdKey { TenantName = settings.MasterTenant, TrackName = settings.MasterTrack, PartyName = settings.ClientId });
            seedClientDownParty.Client = new OAuthDownClient
            {
                RedirectUris = new[] { settings.RedirectUri }.ToList(),
                ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = apiResourceName, Scopes = new[] { apiResourceMasterScope }.ToList() } },
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

        private async Task<(string, OAuthClientSecret)> CreateSecretAsync()
        {
            var oauthClientSecret = new OAuthClientSecret();
            var secret = RandomGenerator.Generate(32);
            await secretHashLogic.AddSecretHashAsync(oauthClientSecret, secret);
            return (secret, oauthClientSecret);
        }
    }
}
