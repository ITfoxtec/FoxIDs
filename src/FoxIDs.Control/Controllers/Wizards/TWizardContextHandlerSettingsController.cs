using AutoMapper;
using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Base, Constants.ControlApi.Segment.Party)]
    public class TWizardContextHandlerSettingsController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly IMapper mapper;
        private readonly DownloadLogic downloadLogic;

        public TWizardContextHandlerSettingsController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, DownloadLogic downloadLogic) : base(logger)
        {
            this.settings = settings;
            this.mapper = mapper;
            this.downloadLogic = downloadLogic;
        }

        /// <summary>
        /// Get wizard ContextHandler settings.
        /// </summary>
        /// <returns>Client settings.</returns>
        [ProducesResponseType(typeof(Api.NewPartyName), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.WizardContextHandlerSettings>> GetWizardContextHandlerSettings()
        {
            try
            {
                var contextHandlerSettings = settings.WizardSettings?.ContextHandler;
                if (contextHandlerSettings == null)
                {
                    throw new Exception("Wizard, ContextHandler settings is not configured.");
                }
                await contextHandlerSettings.ValidateObjectAsync();

                return new Api.WizardContextHandlerSettings
                {
                    OioSaml3MetadataTest = contextHandlerSettings.OioSaml3MetadataTest,
                    OioSaml3MetadataProduction = contextHandlerSettings.OioSaml3MetadataProduction
                };
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read wizard ContextHandler settings.", ex);
            }
        }
    }
}
