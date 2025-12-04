using AutoMapper;
using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Basic, Constants.ControlApi.Segment.Party)]
    public class TReadCertificateController : ApiController
    {
        private readonly IMapper mapper;

        public TReadCertificateController(TelemetryScopedLogger logger, IMapper mapper) : base(logger, auditLogEnabled: false)
        {
            this.mapper = mapper;
        }

        /// <summary>
        /// Read JWK with certificate information from PFX.
        /// </summary>
        /// <param name="certificateAndPassword">Base64 URL encode certificate and optionally password.</param>
        [ProducesResponseType(typeof(Api.JwkWithCertificateInfo), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.JwkWithCertificateInfo>> PostReadCertificate([FromBody] Api.CertificateAndPassword certificateAndPassword)
        {
            if (!await ModelState.TryValidateObjectAsync(certificateAndPassword)) return BadRequest(ModelState);

            try
            {
                var certificate = LoadCertificateWithFallback(WebEncoders.Base64UrlDecode(certificateAndPassword.EncodeCertificate), certificateAndPassword.Password);

                if (!certificateAndPassword.Password.IsNullOrWhiteSpace() && !certificate.HasPrivateKey)
                {
                    throw new ValidationException("Unable to read the certificates private key. E.g, try to convert the certificate and save the certificate with 'TripleDES-SHA1'.");
                }

                var jwt = await certificate.ToFTJsonWebKeyAsync(includePrivateKey: true);
                return Ok(mapper.Map<Api.JwkWithCertificateInfo>(jwt));
            }
            catch (CryptographicException cex)
            {
                if (OperatingSystem.IsWindows())
                {
                    throw new CryptographicException("Unable to read the certificate. Try to convert the certificate and save the certificate with 'TripleDES-SHA1'.", cex);
                }
                else
                {
                    throw new CryptographicException("Unable to read the certificate.", cex);
                }
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read certificate.", ex);
            }
        }

        private static X509Certificate2 LoadCertificateWithFallback(byte[] certificateBytes, string? password)
        {
            try
            {
                var primaryFlags = X509KeyStorageFlags.Exportable;
                if (OperatingSystem.IsWindows())
                {
                    primaryFlags |= X509KeyStorageFlags.MachineKeySet;
                    primaryFlags |= X509KeyStorageFlags.PersistKeySet;
                }
                else
                {
                    primaryFlags |= X509KeyStorageFlags.EphemeralKeySet;
                }
                return LoadWithFlags(certificateBytes, password, primaryFlags);
            }
            catch (CryptographicException primaryEx) 
            {
                try
                {
                    var fallbackFlags = X509KeyStorageFlags.Exportable;
                    if (OperatingSystem.IsWindows())
                    {
                        fallbackFlags |= X509KeyStorageFlags.EphemeralKeySet;
                    }
                    return LoadWithFlags(certificateBytes, password, fallbackFlags);
                }
                catch (CryptographicException)
                {
                    throw primaryEx;
                }           
            }
        }

        private static X509Certificate2 LoadWithFlags(byte[] certificateBytes, string password, X509KeyStorageFlags keyStorageFlags)
        {
            if (password.IsNullOrWhiteSpace())
            {
                try
                {
                    return X509CertificateLoader.LoadCertificate(certificateBytes);
                }
                catch (CryptographicException)
                {
                    return X509CertificateLoader.LoadPkcs12(certificateBytes, string.Empty, keyStorageFlags, Pkcs12LoaderLimits.Defaults);
                }
            }

            return X509CertificateLoader.LoadPkcs12(certificateBytes, password, keyStorageFlags, Pkcs12LoaderLimits.Defaults);
        }
    }
}
