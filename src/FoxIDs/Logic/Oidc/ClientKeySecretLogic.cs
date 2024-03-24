using FoxIDs.Models;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.IdentityModel.Tokens;
using ITfoxtec.Identity;
using System.Linq;

namespace FoxIDs.Logic
{
    public class ClientKeySecretLogic<TClient> : LogicSequenceBase where TClient : OAuthUpClient
    {
        private readonly FoxIDsSettings settings;
        private readonly ExternalKeyLogic externalKeyLogic;

        public ClientKeySecretLogic(FoxIDsSettings settings, ExternalKeyLogic externalKeyLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.externalKeyLogic = externalKeyLogic;
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

            return externalKeyLogic.GetExternalRSAKey(clientKey).ToSecurityKey(clientKey.PublicKey.Kid);
        }
    }
}
