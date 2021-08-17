using FoxIDs.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Api = FoxIDs.Models.Api;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;

namespace FoxIDs.Controllers
{
    public class TConvertCertificateController : TenantApiController
    {
        public TConvertCertificateController(TelemetryScopedLogger logger) : base(logger)
        { }

        /// <summary>
        /// Convert certificate.
        /// </summary>
        /// <param name="certificateRequest">Certificate to read.</param>
        /// <returns>Converted certificate.</returns>
        [ProducesResponseType(typeof(Api.Track), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.ConvertCertificateResponse>> PostConvertCertificate([FromBody] Api.ConvertCertificateRequest certificateRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(certificateRequest)) return BadRequest(ModelState);

            var certificateRequestBytes = Convert.FromBase64String(certificateRequest.Bytes);

            var jwk = certificateRequest.Password.IsNullOrWhiteSpace() switch
            {
                true => await ConvertCertificateToJwk(certificateRequestBytes, string.Empty),
                false => await ConvertCertificateToJwk(certificateRequestBytes, certificateRequest.Password)
            };

            return Ok(new Api.ConvertCertificateResponse { Key = jwk });
        }

        private async Task<JsonWebKey> ConvertCertificateToJwk(byte[] certificateBytes, string password)
        {
            var certificate = new X509Certificate2(certificateBytes, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            return await certificate.ToFTJsonWebKeyAsync(true);
        } 
    }
}
