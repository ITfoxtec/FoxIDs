using Azure.Core;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.IdentityModel.Tokens;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using RSAKeyVaultProvider;
using System.Security.Cryptography;

namespace FoxIDs.Logic
{
    public class ClientKeySecretLogic<TClient> : LogicSequenceBase where TClient : OidcUpClient
    {
        private readonly FoxIDsSettings settings;
        private readonly TokenCredential tokenCredential;

        public ClientKeySecretLogic(FoxIDsSettings settings,TokenCredential tokenCredential, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.tokenCredential = tokenCredential;
        }

        public SecurityKey GetClientKey(TClient client)
        {
            if (client.ClientKey == null)
            {
                throw new ArgumentException("Client key is null.");
            }

            var certificate = client.ClientKey.Key.ToX509Certificate();
            certificate.ValidateCertificate($"Client (client id '{client.ClientId}') key");

            return GetPrimaryRSAKeyVault(client.ClientKey).ToSecurityKey(client.ClientKey.Key.Kid);
        }

        private RSA GetPrimaryRSAKeyVault(ClientKey clientKey)
        {
            return RSAFactory.Create(tokenCredential, new Uri(UrlCombine.Combine(settings.KeyVault.EndpointUri, "keys", clientKey.ExternalName, clientKey.ExternalId)), new Azure.Security.KeyVault.Keys.JsonWebKey(clientKey.Key.ToRsa()));
        }
    }
}
