using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;
using ITfoxtec.Identity;
using FoxIDs.Util;
using System.Text.RegularExpressions;

namespace FoxIDs.Logic
{
    public class ValidateApiModelDynamicElementLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ValidateApiModelDynamicElementLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public bool ValidateApiModelCreateUserElements(ModelStateDictionary modelState, List<Api.DynamicElement> createUserElements, bool disablePasswordAuth, bool enablePasswordlessEmail, bool enablePasswordlessSms)
        {
            var isValid = true;
            try
            {
                if (createUserElements?.Count() > 0)
                {
                    AddDefaultNames(createUserElements);
                    ValidateRegEx(createUserElements);
                    ValidateDuplicatedOrderNumber(createUserElements);
                    ValidateDuplicatedElementType(createUserElements);

                    if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.EmailAndPassword).Count() >= 1)
                    {
                        if (disablePasswordAuth)
                        {
                            throw new ValidationException($"The dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} can not be used when password authentication is disabled.");
                        }
                        else
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Email || e.Type == Api.DynamicElementTypes.Phone || e.Type == Api.DynamicElementTypes.Username || e.Type == Api.DynamicElementTypes.Password).Count() >= 1)
                            {
                                throw new ValidationException($"The dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} can not be combined with dynamic elements of type {nameof(Api.DynamicElementTypes.Email)} or {nameof(Api.DynamicElementTypes.Phone)} or {nameof(Api.DynamicElementTypes.Username)} or {nameof(Api.DynamicElementTypes.Password)}.");
                            }
                        }

                        if (enablePasswordlessEmail || enablePasswordlessSms)
                        {
                            throw new ValidationException($"The dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} can not be used with passwordless with email or passwordless with SMS.");
                        }
                    }
                    else
                    {
                        if (disablePasswordAuth)
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Password).Count() >= 1)
                            {
                                throw new ValidationException($"The dynamic element of type {nameof(Api.DynamicElementTypes.Password)} can not be used when password authentication is disabled.");
                            }
                        }
                        else
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Password).Count() != 1)
                            {
                                throw new ValidationException($"One dynamic element of type {nameof(Api.DynamicElementTypes.Password)} is mandatory.");
                            }
                        }

                        if (enablePasswordlessEmail)
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Email && e.Required).Count() != 1)
                            {
                                throw new ValidationException($"One dynamic element set as required of type {nameof(Api.DynamicElementTypes.Email)} is mandatory using passwordless with email.");
                            }
                        }

                        if (enablePasswordlessSms)
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Phone && e.Required).Count() != 1)
                            {
                                throw new ValidationException($"One dynamic element set as required of type {nameof(Api.DynamicElementTypes.Phone)} is mandatory using passwordless with SMS.");
                            }
                        }

                        if (createUserElements.Where(e => (e.Type == Api.DynamicElementTypes.Email || e.Type == Api.DynamicElementTypes.Phone || e.Type == Api.DynamicElementTypes.Username) && e.Required).Count() < 1)
                        {
                            throw new ValidationException($"At least one dynamic element of type {nameof(Api.DynamicElementTypes.Email)} or {nameof(Api.DynamicElementTypes.Phone)} or {nameof(Api.DynamicElementTypes.Username)} must be set as required.");
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
                    AddDefaultNames(linkExternalUserElements);
                    ValidateRegEx(linkExternalUserElements);
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

        public bool ValidateApiModelExtendedUiElements(ModelStateDictionary modelState, List<Api.DynamicElement> extendedUiElements)
        {
            var isValid = true;
            try
            {
                if (extendedUiElements?.Count() > 0)
                {
                    AddDefaultNames(extendedUiElements);
                    ValidateRegEx(extendedUiElements);
                    ValidateDuplicatedOrderNumber(extendedUiElements);
                    ValidateDuplicatedElementType(extendedUiElements);

                    if (extendedUiElements.Where(e => e.Type == Api.DynamicElementTypes.EmailAndPassword).Count() == 1)
                    {
                        throw new ValidationException($"Dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} is not supported.");
                    }
                    if (extendedUiElements.Where(e => e.Type == Api.DynamicElementTypes.Password).Count() == 1)
                    {
                        throw new ValidationException($"Dynamic element of type {nameof(Api.DynamicElementTypes.Password)} is not supported.");
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError(nameof(Api.ExtendedUi.Elements).ToCamelCase(), vex.Message);
            }
            return isValid;
        }

        private void AddDefaultNames(List<Api.DynamicElement> elements)
        {
            foreach (var element in elements)
            {
                if (element.Name.IsNullOrWhiteSpace())
                {
                    element.Name = GenerateName(elements);
                }
            }
        }
        private string GenerateName(List<Api.DynamicElement> elements, int count = 0)
        {
            var name = RandomName.GenerateDefaultName();
            if (count < Constants.Models.DefaultNameMaxAttempts)
            {
                if (elements.Where(e => e.Name == name).Any()) 
                {
                    count++;
                    return GenerateName(elements, count: count);
                }
            }
            return name;
        }

        private static void ValidateDuplicatedOrderNumber(List<Api.DynamicElement> dynamicElement)
        {
            var duplicatedOrderNumber = dynamicElement.GroupBy(ct => ct.Order as int?).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
            if (duplicatedOrderNumber >= 0)
            {
                throw new ValidationException($"Duplicated create user dynamic element order number '{duplicatedOrderNumber}'");
            }
        }

        private static void ValidateDuplicatedElementType(List<Api.DynamicElement> dynamicElement)
        {
            var duplicatedElementType = dynamicElement.Where(ct => ct.Type != Api.DynamicElementTypes.Custom && ct.Type != Api.DynamicElementTypes.Text && ct.Type != Api.DynamicElementTypes.Html)
                .GroupBy(ct => ct.Type).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
            if (duplicatedElementType > 0)
            {
                throw new ValidationException($"Duplicated create user dynamic element type '{duplicatedElementType}'");
            }
        }

        private static void ValidateRegEx(List<Api.DynamicElement> dynamicElement)
        {
            foreach (var element in dynamicElement)
            {
                if (!element.RegEx.IsNullOrWhiteSpace())
                {
                    try
                    {
                        Regex.Match("", element.RegEx);
                    }
                    catch (Exception ex)
                    {
                        throw new ValidationException($"Invalid regex pattern in dynamic element '{element.Name}'.", ex);
                    }
                }
            }
        }
    }
}
