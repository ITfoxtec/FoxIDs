using AutoMapper;
using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Controllers
{
    public class TReadCertificateController : TenantApiController
    {
        private readonly IMapper mapper;

        public TReadCertificateController(TelemetryScopedLogger logger, IMapper mapper) : base(logger)
        {
            this.mapper = mapper;
        }

        /// <summary>
        /// Get JWT with certificate information.
        /// </summary>
        /// <param name="certificateAndPassword">Base64 url encode certificate and optionally password.</param>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.JwtWithCertificateInfo), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.JwtWithCertificateInfo>> PostReadCertificate([FromBody] Api.CertificateAndPassword certificateAndPassword)
        {
            if (!await ModelState.TryValidateObjectAsync(certificateAndPassword)) return BadRequest(ModelState);

            try
            {
                var certificate = certificateAndPassword.Password.IsNullOrWhiteSpace() switch
                {
                    true => new X509Certificate2(WebEncoders.Base64UrlDecode(certificateAndPassword.EncodeCertificate), string.Empty, keyStorageFlags: X509KeyStorageFlags.Exportable),
                    false => new X509Certificate2(WebEncoders.Base64UrlDecode(certificateAndPassword.EncodeCertificate), certificateAndPassword.Password, keyStorageFlags: X509KeyStorageFlags.Exportable),
                };

                var jwt = await certificate.ToFTJsonWebKeyAsync(includePrivateKey: true);
                return Ok(mapper.Map<Api.JwtWithCertificateInfo>(jwt));
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read certificate.", ex);
            }
        }
    }
}
