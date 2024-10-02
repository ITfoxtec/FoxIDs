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

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Base, Constants.ControlApi.Segment.Party)]
    public class TWizardContextHandlerSettingsController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly IMapper mapper;

        public TWizardContextHandlerSettingsController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper) : base(logger)
        {
            this.settings = settings;
            this.mapper = mapper;
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

                return mapper.Map<Api.WizardContextHandlerSettings>(contextHandlerSettings);
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read wizard ContextHandler settings.", ex);
            }
        }
    }
}