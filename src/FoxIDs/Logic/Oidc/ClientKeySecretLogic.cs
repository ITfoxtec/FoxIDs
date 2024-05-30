using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.IdentityModel.Tokens;
using ITfoxtec.Identity;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs.Logic
{
    public class ClientKeySecretLogic<TClient> : LogicSequenceBase where TClient : OAuthUpClient
    {
        private readonly IServiceProvider serviceProvider;

        public ClientKeySecretLogic(IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.serviceProvider = serviceProvider;
        }

        public SecurityKey GetClientKey(TClient client)
        {
            if (!(client.ClientKeys?.Count > 0))
            {
                throw new ArgumentException("Client key is null.");
            }

            var clientKey = client.ClientKeys.First();
            var certificate = clientKey.PublicKey.ToX509Certificate();
            certificate.ValidateCertificate($"Client (client id '{client.ClientId}') key");

            if (clientKey.Type == ClientKeyTypes.Contained)
            {
                return clientKey.Key.ToSecurityKey();
            }
            else if (clientKey.Type == ClientKeyTypes.KeyVaultImport)
            {
                return serviceProvider.GetService<ExternalKeyLogic>().GetExternalRSAKey(clientKey).ToSecurityKey(clientKey.PublicKey.Kid);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
