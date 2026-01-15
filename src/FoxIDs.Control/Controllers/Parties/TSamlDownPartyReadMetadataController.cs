using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public class TSamlDownPartyReadMetadataController : ApiController
    {
        private readonly IMapper mapper;
        private readonly SamlMetadataReadLogic samlMetadataReadLogic;

        public TSamlDownPartyReadMetadataController(TelemetryScopedLogger logger, IMapper mapper, SamlMetadataReadLogic samlMetadataReadLogic) : base(logger)
        {
            this.mapper = mapper;
            this.samlMetadataReadLogic = samlMetadataReadLogic;
        }

        /// <summary>
        /// Read SAML 2.0 application registration metadata.
        /// </summary>
        /// <param name="samlReadMetadataRequest">SAML 2.0 metadata.</param>
        /// <returns>SAML 2.0 application registration.</returns>
        [ProducesResponseType(typeof(Api.SamlDownParty), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.SamlDownParty>> PostSamlDownPartyReadMetadata([FromBody] Api.SamlReadMetadataRequest samlReadMetadataRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(samlReadMetadataRequest)) return BadRequest(ModelState);

            try
            {
                var samlDownParty = new SamlDownParty { AuthnBinding = new SamlBinding() };
                switch (samlReadMetadataRequest.Type)
                {
                    case Api.SamlReadMetadataType.Url:
                        samlDownParty.MetadataUrl = samlReadMetadataRequest.Metadata;
                        samlDownParty = await samlMetadataReadLogic.PopulateModelAsync(samlDownParty);
                        break;
                    case Api.SamlReadMetadataType.Xml:
                        samlDownParty = await samlMetadataReadLogic.PopulateModelAsync(samlDownParty, samlReadMetadataRequest.Metadata);
                        break;
                    default:
                        throw new NotSupportedException();
                }
                return Ok(mapper.Map<Api.SamlDownParty>(samlDownParty));
            }
            catch (Exception ex)
            {
                throw new ValidationException("Unable to read SAML 2.0 metadata.", ex);
            }
        }
    }
}
