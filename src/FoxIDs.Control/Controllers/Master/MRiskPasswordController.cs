using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using OpenSearch.Client;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class MRiskPasswordController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;

        public MRiskPasswordController(TelemetryScopedLogger logger, IMapper mapper, IMasterDataRepository masterDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
        }

        /// <summary>
        /// Get risk password.
        /// </summary>
        /// <param name="passwordSha1Hash">Password SHA1 hash.</param>
        /// <returns>Risk password.</returns>
        [ProducesResponseType(typeof(Api.RiskPassword), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.RiskPassword>> GetRiskPassword(string passwordSha1Hash)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(passwordSha1Hash, nameof(passwordSha1Hash))) return BadRequest(ModelState);

                var mRiskPassword = await masterDataRepository.GetAsync<RiskPassword>(await RiskPassword.IdFormatAsync(passwordSha1Hash));
                return Ok(mapper.Map<Api.RiskPassword>(mRiskPassword));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.RiskPassword).Name}' by password SHA1 hash '{passwordSha1Hash}'.");
                    return NotFound(typeof(Api.RiskPassword).Name, passwordSha1Hash);
                }
                throw;
            }
        }

        /// <summary>
        /// Update risk passwords.
        /// </summary>
        /// <param name="riskPasswordRequest">Risk passwords.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PutRiskPassword([FromBody] Api.RiskPasswordRequest riskPasswordRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var riskPasswords = new List<RiskPassword>();
            foreach (var item in riskPasswordRequest.RiskPasswords)
            {
                riskPasswords.Add(new RiskPassword
                {
                    Id = await RiskPassword.IdFormatAsync(item.PasswordSha1Hash),
                    Count = item.Count,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }

            await masterDataRepository.SaveListAsync(riskPasswords);

            return NoContent();
        }

        /// <summary>
        /// Delete risk passwords.
        /// </summary>
        /// <param name="riskPasswordDelete">Delete specified risk passwords.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRiskPassword([FromBody] Api.RiskPasswordDelete riskPasswordDelete)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ids = new List<string>();
            foreach (var passwordSha1Hash in riskPasswordDelete.PasswordSha1Hashs)
            {
                ids.Add(await RiskPassword.IdFormatAsync(passwordSha1Hash));
            }

            await masterDataRepository.DeleteListAsync<RiskPassword>(ids);

            return NoContent();
        }
    }
}
