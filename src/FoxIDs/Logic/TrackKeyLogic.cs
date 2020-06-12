using System;
using System.Security.Cryptography;
using Azure.Core;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using RSAKeyVaultProvider;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class TrackKeyLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TokenCredential tokenCredential;

        public TrackKeyLogic(FoxIDsSettings settings, TokenCredential tokenCredential, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.tokenCredential = tokenCredential;
        }

        public SecurityKey GetSecurityKey(TrackKey trackKey)
        {
            ValidateTrackKey(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyType.Contained:
                    return trackKey.Key;

                case TrackKeyType.KeyVault:
                    return new RsaSecurityKey(GetRSAKeyVault(trackKey));

                default:
                    throw new NotSupportedException($"Track key type '{trackKey.Type}' not supported.");
            }
        }

        public Saml2X509Certificate GetSaml2X509Certificate(TrackKey trackKey)
        {
            ValidateTrackKey(trackKey);

            switch (trackKey.Type)
            {
                case TrackKeyType.Contained:
                    return trackKey.Key.ToSaml2X509Certificate(true);

                case TrackKeyType.KeyVault:
                    return new Saml2X509Certificate(trackKey.Key.ToX509Certificate(), GetRSAKeyVault(trackKey));

                default:
                    throw new NotSupportedException($"Track key type '{trackKey.Type}' not supported.");
            }
        }

        private RSA GetRSAKeyVault(TrackKey trackKey)
        {
            return RSAFactory.Create(tokenCredential, new Uri(UrlCombine.Combine(settings.KeyVault.EndpointUri, "keys", trackKey.ExternalName)), new Azure.Security.KeyVault.Keys.JsonWebKey(trackKey.Key.ToRsa()));
        }

        private void ValidateTrackKey(TrackKey trackKey)
        {
            var nowLocal = DateTime.Now;
            var certificate = trackKey.Key.ToX509Certificate();
            if (certificate.NotBefore > nowLocal)
            {
                throw new Exception($"Track certificate not valid yet. NotBefore {certificate.NotBefore.ToUniversalTime().ToString("u")}.");
            }
            if (certificate.NotAfter < nowLocal)
            {
                throw new Exception($"Track certificate is expired. NotAfter {certificate.NotAfter.ToUniversalTime().ToString("u")}.");
            }
        }
    }
}
