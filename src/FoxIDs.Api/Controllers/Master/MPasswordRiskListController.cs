using FoxIDs.Infrastructure;
using FoxIDs.Model;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    //[ApiExplorerSettings(GroupName = "PasswordRiskList")]
    [Route("@master/!mpasswordrisklist")]
    public class MPasswordRiskListController : MasterApiController
    {
        private readonly TelemetryLogger logger;
        private readonly IMasterRepository masterService;

        public MPasswordRiskListController(TelemetryLogger logger, IMasterRepository masterService) : base(logger)
        {
            this.logger = logger;
            this.masterService = masterService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RiskPasswordApiModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var riskPasswords = new List<RiskPassword>();
            foreach (var item in model.RiskPasswords)
            {
                riskPasswords.Add(new RiskPassword
                {
                    Id = RiskPassword.IdFormat(new RiskPassword.IdKey { PasswordSha1Hash = item.PasswordSha1Hash }),
                    Count = item.Count,
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }

            await masterService.SaveBulkAsync(riskPasswords);

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string passwordSha1Hash)
        {
            var passwordRiskList = new RiskPassword { Id = RiskPassword.IdFormat(new RiskPassword.IdKey { PasswordSha1Hash = passwordSha1Hash }) };
            await masterService.DeleteAsync(passwordRiskList);

            return NoContent();
        }
    }
}
