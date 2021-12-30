using AutoMapper;
using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Logic;
using FoxIDs.Models;

namespace FoxIDs.Controllers
{
    public class TSamlUpPartyReadMetadataController : TenantApiController
    {
        private readonly IMapper mapper;
        private readonly SamlMetadataReadLogic samlMetadataReadLogic;

        public TSamlUpPartyReadMetadataController(TelemetryScopedLogger logger, IMapper mapper, SamlMetadataReadLogic samlMetadataReadLogic) : base(logger)
        {
            this.mapper = mapper;
            this.samlMetadataReadLogic = samlMetadataReadLogic;
        }

        /// <summary>
        /// Read saml 2.0 up-party metadata.
        /// </summary>
        /// <param name="metadataXml">SAML 2.0 metadata XML.</param>
        /// <returns>SAML 2.0 up-party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.SamlUpParty>> PostSamlUpPartyReadMetadata([FromBody] string metadataXml)
        {
            if (!ModelState.TryValidateRequiredParameter(metadataXml, nameof(metadataXml))) return BadRequest(ModelState);

            try
            {
                var samlUpParty = new SamlUpParty { AuthnBinding = new SamlBinding() };
                await samlMetadataReadLogic.PopulateModelAsync(samlUpParty, metadataXml);
                return Ok(mapper.Map<Api.SamlUpParty>(samlUpParty));
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read SAML 2.0 metadata.", ex);
            }
        }
    }
}
