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
using FoxIDs.Models.Config;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Base, Constants.ControlApi.Segment.Party)]
    public class TWizardNemLoginSettingsController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly IMapper mapper;
        private readonly DownloadLogic downloadLogic;

        public TWizardNemLoginSettingsController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, DownloadLogic downloadLogic) : base(logger)
        {
            this.settings = settings;
            this.mapper = mapper;
            this.downloadLogic = downloadLogic;
        }

        /// <summary>
        /// Get wizard NemLogin settings.
        /// </summary>
        /// <returns>Client settings.</returns>
        [ProducesResponseType(typeof(Api.NewPartyName), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.WizardNemLoginSettings>> GetWizardNemLoginSettings()
        {
            try
            {
                var nemLoginSettings = settings.WizardSettings?.NemLogin;
                if (nemLoginSettings == null)
                {
                    throw new Exception("Wizard, NemLogin settings is not configured.");
                }
                await nemLoginSettings.ValidateObjectAsync();

                var result = mapper.Map<Api.WizardNemLoginSettings>(nemLoginSettings);

                var certBytes = await downloadLogic.DownloadAsBytesAsync(nemLoginSettings.Oces3TestCertificateUrl, "OCES3 test certificate");
                var oces3TestCertificate = new X509Certificate2(certBytes, nemLoginSettings.Oces3TestCertificatePasswrod, keyStorageFlags: X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                if (!oces3TestCertificate.HasPrivateKey)
                {
                    throw new ValidationException("Unable to read the OCES3 test certificate private key.");
                }
                result.Oces3TestCertificate = mapper.Map<Api.JwkWithCertificateInfo>(await oces3TestCertificate.ToFTJsonWebKeyAsync(includePrivateKey: true));

                return result;
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read wizard NemLogin settings.", ex);
            }
        }
    }
}
