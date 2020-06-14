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

namespace FoxIDs.Controllers
{
    public class MPasswordRiskListController : MasterApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMasterRepository masterService;

        public MPasswordRiskListController(TelemetryScopedLogger logger, IMasterRepository masterService) : base(logger)
        {
            this.logger = logger;
            this.masterService = masterService;
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PostPasswordRiskList([FromBody] Api.RiskPassword model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var riskPasswords = new List<RiskPassword>();
            foreach (var item in model.RiskPasswords)
            {
                riskPasswords.Add(new RiskPassword
                {
                    Id = await RiskPassword.IdFormat(new RiskPassword.IdKey { PasswordSha1Hash = item.PasswordSha1Hash }),
                    Count = item.Count,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }

            await masterService.SaveBulkAsync(riskPasswords);

            return NoContent();
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePasswordRiskList(string passwordSha1Hash)
        {
            try
            {
                //TODO delete all elements
                // Waiting for https://feedback.azure.com/forums/263030-azure-cosmos-db/suggestions/17296813-add-the-ability-to-delete-all-data-in-a-partition
                //            Add the ability to delete ALL data in a partition
                var passwordRiskList = new RiskPassword { Id = await RiskPassword.IdFormat(new RiskPassword.IdKey { PasswordSha1Hash = passwordSha1Hash }) };
                await masterService.DeleteAsync(passwordRiskList);
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Delete by password sha1 hash '{passwordSha1Hash}'.");
                    return NotFound();
                }
                throw;
            }
        }
    }
}
