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

        public MClientSettingsController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper) : base(logger, auditLogEnabled: false)
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
            var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? GetType().Assembly;
            var displayVersion = assembly.GetDisplayVersion();
            var majorMinorVersion = assembly.GetName().Version?.ToString(2) ?? string.Empty;

            return Ok(new Api.ControlClientSettings 
            {
                FoxIDsEndpoint = settings.FoxIDsEndpoint,
                Version = majorMinorVersion,
                FullVersion = displayVersion ?? majorMinorVersion,
                LogOption = mapper.Map<Api.LogOptions>(settings.Options.Log),
                KeyStorageOption = mapper.Map<Api.KeyStorageOptions>(settings.Options.KeyStorage),
                EnableCreateNewTenant = !settings.MainTenantSeedEnabled,
                EnablePayment = settings.Payment?.EnablePayment == true && settings.Usage?.EnableInvoice == true,
                PaymentTestMode = settings.Payment != null ? settings.Payment.TestMode : true,
                MollieProfileId = settings.Payment?.MollieProfileId
            });
        }
    }
}
