using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace FoxIDs.Logic
{
    public class ValidateLoginPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateLoginPartyLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.LoginUpParty party)
        {
            var isValid = true;

            if (!party.Css.IsNullOrWhiteSpace())
            {
                //TODO add validation
            }

            if (!party.IconUrl.IsNullOrWhiteSpace())
            {
                try
                {                   
                    var iconExtension = Path.GetExtension(party.IconUrl.Split('?')[0]);
                    _ = iconExtension switch
                    {
                        ".ico" => "image/x-icon",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        ".jpeg" => "image/jpeg",
                        ".webp" => "image/webp",
                        _ => throw new ValidationException($"Icon image format '{iconExtension}' not supported.")
                    };
                }
                catch (ValidationException vex)
                {
                    isValid = false;
                    logger.Warning(vex);
                    modelState.TryAddModelError($"{nameof(Api.LoginUpParty.IconUrl)}".ToCamelCase(), vex.Message);
                }
            }

            return isValid;
        }
    }
}
