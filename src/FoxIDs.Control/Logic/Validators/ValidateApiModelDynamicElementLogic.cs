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

        public bool ValidateApiModelCreateUserElements(ModelStateDictionary modelState, bool passwordless, List<Api.DynamicElement> createUserElements)
        {
            var isValid = true;
            try
            {
                if (createUserElements?.Count() > 0)
                {
                    ValidateDuplicatedOrderNumber(createUserElements);
                    ValidateDuplicatedElementType(createUserElements);

                    if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.EmailAndPassword).Count() >= 1)
                    {
                        if (passwordless)
                        {
                            throw new ValidationException($"The dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} can not be used with passwordless.");
                        }
                        else
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Email || e.Type == Api.DynamicElementTypes.Phone || e.Type == Api.DynamicElementTypes.Username || e.Type == Api.DynamicElementTypes.Password).Count() >= 1)
                            {
                                throw new ValidationException($"The dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} can not be combined with dynamic elements of type {nameof(Api.DynamicElementTypes.Email)} or {nameof(Api.DynamicElementTypes.Phone)} or {nameof(Api.DynamicElementTypes.Username)} or {nameof(Api.DynamicElementTypes.Password)}.");
                            }
                        }
                    }
                    else
                    {
                        if (createUserElements.Where(e => (e.Type == Api.DynamicElementTypes.Email || e.Type == Api.DynamicElementTypes.Phone || e.Type == Api.DynamicElementTypes.Username) && e.Required).Count() < 1)
                        {
                            throw new ValidationException($"At least one dynamic element of type {nameof(Api.DynamicElementTypes.Email)} or {nameof(Api.DynamicElementTypes.Phone)} or {nameof(Api.DynamicElementTypes.Username)} must be set as required.");
                        }

                        if (passwordless)
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Password).Count() >= 1)
                            {
                                throw new ValidationException($"The dynamic element of type {nameof(Api.DynamicElementTypes.Password)} can not be used with passwordless.");
                            }
                        }
                        else
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Password).Count() != 1)
                            {
                                throw new ValidationException($"One dynamic element of type {nameof(Api.DynamicElementTypes.Password)} is mandatory.");
                            }
                        }
                    }
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
                    ValidateDuplicatedElementType(linkExternalUserElements);

                    if (linkExternalUserElements.Where(e => e.Type == Api.DynamicElementTypes.EmailAndPassword).Count() == 1)
                    {
                        throw new ValidationException($"Dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} is not supported.");
                    }
                    if (linkExternalUserElements.Where(e => e.Type == Api.DynamicElementTypes.Password).Count() == 1)
                    {
                        throw new ValidationException($"Dynamic element of type {nameof(Api.DynamicElementTypes.Password)} is not supported.");
                    }
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
