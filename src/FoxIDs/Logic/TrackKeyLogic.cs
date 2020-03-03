using System;
using System.Security.Cryptography;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Tokens;

namespace FoxIDs.Logic
{
    public class TrackKeyLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly KeyVaultClient keyVaultClient;

        public TrackKeyLogic(FoxIDsSettings settings, KeyVaultClient keyVaultClient, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.keyVaultClient = keyVaultClient;
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
            return keyVaultClient.ToRSA(new KeyIdentifier(settings.KeyVault.EndpointUri, trackKey.ExternalName), new Microsoft.Azure.KeyVault.WebKey.JsonWebKey(trackKey.Key.ToRsaParameters()));
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
