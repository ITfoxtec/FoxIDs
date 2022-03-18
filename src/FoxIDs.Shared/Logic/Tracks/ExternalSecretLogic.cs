using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Azure.Core;
using FoxIDs.Models.Config;
using Azure.Security.KeyVault.Secrets;

namespace FoxIDs.Logic
{
    public class ExternalSecretLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly TokenCredential tokenCredential;

        public ExternalSecretLogic(Settings settings, TokenCredential tokenCredential, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.tokenCredential = tokenCredential;
        }

        public async Task<string> CreateExternalSecretAsync(string name, string value)
        {
            var externalName = $"{name}-{Guid.NewGuid()}";

            var keyVaultSecret = new KeyVaultSecret(GetExternalFullName(externalName), value);
            var secretClient = new SecretClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            await secretClient.SetSecretAsync(keyVaultSecret);

            return externalName;
        }

        public async Task<string> GetExternalSecretAsync(string externalName)
        {
            var secretClient = new SecretClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            var keyVaultSecret = await secretClient.GetSecretAsync(GetExternalFullName(externalName));
            return keyVaultSecret?.Value?.Value;
        }

        public async Task DeleteExternalSecretAsync(string externalName)
        {
            var secretClient = new SecretClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            await secretClient.StartDeleteSecretAsync(GetExternalFullName(externalName));
        }

        private string GetExternalFullName(string externalName)
        {
            return $"{RouteBinding.TenantName}-{RouteBinding.TrackName}-{externalName}";
        }
    }
}
