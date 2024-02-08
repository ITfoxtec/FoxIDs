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
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public class TSamlUpPartyReadMetadataController : ApiController
    {
        private readonly IMapper mapper;
        private readonly SamlMetadataReadLogic samlMetadataReadLogic;

        public TSamlUpPartyReadMetadataController(TelemetryScopedLogger logger, IMapper mapper, SamlMetadataReadLogic samlMetadataReadLogic) : base(logger)
        {
            this.mapper = mapper;
            this.samlMetadataReadLogic = samlMetadataReadLogic;
        }

        /// <summary>
        /// Read SAML 2.0 up-party metadata.
        /// </summary>
        /// <param name="samlReadMetadataRequest">SAML 2.0 metadata.</param>
        /// <returns>SAML 2.0 up-party.</returns>
        [ProducesResponseType(typeof(Api.SamlUpParty), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.SamlUpParty>> PostSamlUpPartyReadMetadata([FromBody] Api.SamlReadMetadataRequest samlReadMetadataRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(samlReadMetadataRequest)) return BadRequest(ModelState);

            try
            {
                var samlUpParty = new SamlUpParty { AuthnBinding = new SamlBinding() };
                switch (samlReadMetadataRequest.Type)
                {
                    case Api.SamlReadMetadataType.Url:
                        samlUpParty.MetadataUrl = samlReadMetadataRequest.Metadata;
                        await samlMetadataReadLogic.PopulateModelAsync(samlUpParty);
                        break;
                    case Api.SamlReadMetadataType.Xml:
                        await samlMetadataReadLogic.PopulateModelAsync(samlUpParty, samlReadMetadataRequest.Metadata);
                        break;
                    default:
                        throw new NotSupportedException();
                }
                return Ok(mapper.Map<Api.SamlUpParty>(samlUpParty));
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read SAML 2.0 metadata.", ex);
            }
        }
    }
}
