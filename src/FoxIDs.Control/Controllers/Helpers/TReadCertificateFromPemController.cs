using AutoMapper;
using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using System.Security.Cryptography.X509Certificates;
using System;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Basic, Constants.ControlApi.Segment.Party)]
    public class TReadCertificateFromPemController : ApiController
    {
        private readonly IMapper mapper;

        public TReadCertificateFromPemController(TelemetryScopedLogger logger, IMapper mapper) : base(logger)
        {
            this.mapper = mapper;
        }

        /// <summary>
        /// Read JWK with certificate information from PEM certificate (.crt) and private key (.key).
        /// </summary>
        /// <param name="certificateCrtAndKey">PEM certificate and private key.</param>
        [ProducesResponseType(typeof(Api.JwkWithCertificateInfo), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.JwkWithCertificateInfo>> PostReadCertificateFromPem([FromBody] Api.CertificateCrtAndKey certificateCrtAndKey)
        {
            if (!await ModelState.TryValidateObjectAsync(certificateCrtAndKey)) return BadRequest(ModelState);

            try
            {
                var certificate = X509Certificate2.CreateFromPem(certificateCrtAndKey.CertificatePemCrt, certificateCrtAndKey.CertificatePemKey);
                var jwt = await certificate.ToFTJsonWebKeyAsync(includePrivateKey: certificate.HasPrivateKey);
                return Ok(mapper.Map<Api.JwkWithCertificateInfo>(jwt));
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read PEM certificate and key.", ex);
            }
        }
    }
}
