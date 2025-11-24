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
                var certificate = certificateAndPassword.Password.IsNullOrWhiteSpace() switch
                {
                    //Can not be change to X509CertificateLoader LoadPkcs12 or LoadCertificate because it should automatically select between the two methods.
                    true => new X509Certificate2(WebEncoders.Base64UrlDecode(certificateAndPassword.EncodeCertificate), string.Empty, keyStorageFlags: X509KeyStorageFlags.Exportable),
                    false => new X509Certificate2(WebEncoders.Base64UrlDecode(certificateAndPassword.EncodeCertificate), certificateAndPassword.Password, keyStorageFlags: X509KeyStorageFlags.Exportable),
                };

                if (!certificateAndPassword.Password.IsNullOrWhiteSpace() && !certificate.HasPrivateKey)
                {
                    throw new ValidationException("Unable to read the certificates private key. E.g, try to convert the certificate and save the certificate with 'TripleDES-SHA1'.");
                }

                var jwt = await certificate.ToFTJsonWebKeyAsync(includePrivateKey: true);
                return Ok(mapper.Map<Api.JwkWithCertificateInfo>(jwt));
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read certificate.", ex);
            }
        }
    }
}