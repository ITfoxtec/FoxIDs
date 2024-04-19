using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace FoxIDs.Controllers.Client
{
    public class MClientSettingsController : ApiController
    {
        private readonly FoxIDsControlSettings settings;
        private readonly IMapper mapper;

        public MClientSettingsController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper) : base(logger)
        {
            this.settings = settings;
            this.mapper = mapper;
        }

        /// <summary>
        /// Get client settings.
        /// </summary>
        /// <returns>Client settings.</returns>
        [ProducesResponseType(typeof(Api.ControlClientSettings), StatusCodes.Status200OK)]
        public ActionResult<Api.ControlClientSettings> GetClientSettings()
        {
            var version = GetType().Assembly.GetName().Version;

            return Ok(new Api.ControlClientSettings 
            {
                FoxIDsEndpoint = settings.FoxIDsEndpoint,
                Version = version.ToString(2),
                FullVersion = version.ToString(3),
                LogOption = mapper.Map<Api.LogOptions>(settings.Options.Log),
                KeyStorageOption = mapper.Map<Api.KeyStorageOptions>(settings.Options.KeyStorage)
            });
        }
    }
}
