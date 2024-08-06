using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.IdentityModel.Tokens;
using ITfoxtec.Identity;
using System.Linq;

namespace FoxIDs.Logic
{
    public class ClientKeySecretLogic<TClient> : LogicSequenceBase where TClient : OAuthUpClient
    {
        public ClientKeySecretLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

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
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
