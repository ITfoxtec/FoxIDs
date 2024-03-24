using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace FoxIDs.Logic
{
    public class ValidateApiModelDynamicElementLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateApiModelDynamicElementLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateApiModelCreateUserElements(ModelStateDictionary modelState, List<Api.DynamicElement> createUserElements)
        {
            var isValid = true;
            try
            {
                if (createUserElements?.Count() > 0)
                {
                    ValidateDuplicatedOrderNumber(createUserElements);

                    if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Email).Count() == 1)
                    {
                        throw new ValidationException("Dynamic element of type Email is not supported.");
                    }

                    if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.EmailAndPassword).Count() != 1)
                    {
                        throw new ValidationException("Exactly one create user dynamic element of type EmailAndPassword is required.");
                    }

                    ValidateDuplicatedElementType(createUserElements);
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError(nameof(Api.CreateUser.Elements).ToCamelCase(), vex.Message);
            }
            return isValid;
        }

        public bool ValidateApiModelLinkExternalUserElements(ModelStateDictionary modelState, List<Api.DynamicElement> linkExternalUserElements)
        {
            var isValid = true;
            try
            {
                if (linkExternalUserElements?.Count() > 0)
                {
                    ValidateDuplicatedOrderNumber(linkExternalUserElements);

                    if (linkExternalUserElements.Where(e => e.Type == Api.DynamicElementTypes.EmailAndPassword).Count() == 1)
                    {
                        throw new ValidationException("Dynamic element of type EmailAndPassword is not supported.");
                    }

                    ValidateDuplicatedElementType(linkExternalUserElements);
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError(nameof(Api.LinkExternalUser.Elements).ToCamelCase(), vex.Message);
            }
            return isValid;
        }

        private static void ValidateDuplicatedOrderNumber(List<Api.DynamicElement> LinkExternalUserElements)
        {
            var duplicatedOrderNumber = LinkExternalUserElements.GroupBy(ct => ct.Order as int?).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
            if (duplicatedOrderNumber >= 0)
            {
                throw new ValidationException($"Duplicated create user dynamic element order number '{duplicatedOrderNumber}'");
            }
        }

        private static void ValidateDuplicatedElementType(List<Api.DynamicElement> LinkExternalUserElements)
        {
            var duplicatedElementType = LinkExternalUserElements.GroupBy(ct => ct.Type).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
            if (duplicatedElementType > 0)
            {
                throw new ValidationException($"Duplicated create user dynamic element type '{duplicatedElementType}'");
            }
        }
    }
}
