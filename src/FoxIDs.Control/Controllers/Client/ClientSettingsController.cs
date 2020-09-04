using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers.Client
{
    public class ClientSettingsController : ApiController
    {
        private readonly FoxIDsControlSettings settings;

        public ClientSettingsController(FoxIDsControlSettings settings, TelemetryScopedLogger logger) : base(logger)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Get client settings.
        /// </summary>
        /// <returns>Client settings.</returns>
        [ProducesResponseType(typeof(Api.ControlClientSettings), StatusCodes.Status200OK)]
        public ActionResult<Api.ControlClientSettings> GetClientSettings()
        {
            return Ok(new Api.ControlClientSettings { FoxIDsEndpoint = settings.FoxIDsEndpoint });
        }
    }
}
