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
                    ValidateHtml(createUserElements);
                    ValidateDuplicatedOrderNumber(createUserElements);
                    ValidateDuplicatedElementType(createUserElements);

                    if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.EmailAndPassword).Count() >= 1)
                    {
                        if (disablePasswordAuth)
                        {
                            throw new ValidationException($"The user-creation dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} can not be used when password authentication is disabled.");
                        }
                        else
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Email || e.Type == Api.DynamicElementTypes.Phone || e.Type == Api.DynamicElementTypes.Username || e.Type == Api.DynamicElementTypes.Password).Count() >= 1)
                            {
                                throw new ValidationException($"The user-creation dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} can not be combined with dynamic elements of type {nameof(Api.DynamicElementTypes.Email)} or {nameof(Api.DynamicElementTypes.Phone)} or {nameof(Api.DynamicElementTypes.Username)} or {nameof(Api.DynamicElementTypes.Password)}.");
                            }
                        }

                        if (enablePasswordlessEmail || enablePasswordlessSms)
                        {
                            throw new ValidationException($"The user-creation dynamic element of type {nameof(Api.DynamicElementTypes.EmailAndPassword)} can not be used with passwordless login with email or passwordless login with SMS.");
                        }
                    }
                    else
                    {
                        if (disablePasswordAuth)
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Password).Count() >= 1)
                            {
                                throw new ValidationException($"The user-creation dynamic element of type {nameof(Api.DynamicElementTypes.Password)} can not be used when password authentication is disabled.");
                            }
                        }
                        else
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Password).Count() != 1)
                            {
                                throw new ValidationException($"A user-creation dynamic element of type {nameof(Api.DynamicElementTypes.Password)} is mandatory.");
                            }
                        }

                        if (enablePasswordlessEmail)
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Email && e.Required).Count() != 1)
                            {
                                throw new ValidationException($"A user-creation dynamic element of type {nameof(Api.DynamicElementTypes.Email)}, set as required, is mandatory when using passwordlesss login with email.");
                            }
                        }

                        if (enablePasswordlessSms)
                        {
                            if (createUserElements.Where(e => e.Type == Api.DynamicElementTypes.Phone && e.Required).Count() != 1)
                            {
                                throw new ValidationException($"A user-creation dynamic element of type {nameof(Api.DynamicElementTypes.Phone)}, set as required, is mandatory when using passwordlesss login withh SMS.");
                            }
                        }

                        if (createUserElements.Where(e => (e.Type == Api.DynamicElementTypes.Email || e.Type == Api.DynamicElementTypes.Phone || e.Type == Api.DynamicElementTypes.Username) && e.Required).Count() < 1)
                        {
                            throw new ValidationException($"At least one user-creation dynamic element of type {nameof(Api.DynamicElementTypes.Email)} or {nameof(Api.DynamicElementTypes.Phone)} or {nameof(Api.DynamicElementTypes.Username)} must be set as required.");
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
                    ValidateHtml(linkExternalUserElements);
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

        public bool ValidateApiModelLoginElements(ModelStateDictionary modelState, List<Api.DynamicElement> elements)
        {
            var isValid = true;
            try
            {
                if (elements?.Count() > 0)
                {
                    AddDefaultNames(elements);
                    ValidateRegEx(elements);
                    ValidateHtml(elements);
                    ValidateDuplicatedOrderNumber(elements);

                    if (elements.Any(e => e.Type != Api.DynamicElementTypes.Text && e.Type != Api.DynamicElementTypes.Html && e.Type != Api.DynamicElementTypes.LoginInput && e.Type != Api.DynamicElementTypes.LoginButton && e.Type != Api.DynamicElementTypes.LoginLink && e.Type != Api.DynamicElementTypes.LoginHrd))
                    {
                        throw new ValidationException($"Only dynamic elements of type '{nameof(Api.DynamicElementTypes.LoginInput)}', '{nameof(Api.DynamicElementTypes.LoginButton)}', '{nameof(Api.DynamicElementTypes.LoginLink)}', '{nameof(Api.DynamicElementTypes.LoginHrd)}', '{nameof(Api.DynamicElementTypes.Text)}' and '{nameof(Api.DynamicElementTypes.Html)}' are supported in the login UI.");
                    }

                    if (elements.Where(e => e.Type == Api.DynamicElementTypes.LoginInput).Count() > 1)
                    {
                        throw new ValidationException("Login UI must max contain one login input element.");
                    }
                    if (elements.Count(e => e.Type == Api.DynamicElementTypes.LoginButton) > 1)
                    {
                        throw new ValidationException("Login UI must max contain one login button element.");
                    }
                    if (elements.Count(e => e.Type == Api.DynamicElementTypes.LoginLink) > 1)
                    {
                        throw new ValidationException("Login UI must max contain one login link element.");
                    }
                    var loginHrdElements = elements.Where(e => e.Type == Api.DynamicElementTypes.LoginHrd).ToList();
                    if (loginHrdElements.Count > 1)
                    {
                        throw new ValidationException("Login UI must max contain one login HRD element.");
                    }

                    if (loginHrdElements.Count == 1)
                    {
                        var loginHrd = loginHrdElements.First();
                        if (elements.Any(e => e.Order > loginHrd.Order && (e.Type == Api.DynamicElementTypes.LoginInput || e.Type == Api.DynamicElementTypes.LoginButton || e.Type == Api.DynamicElementTypes.LoginLink)))
                        {
                            throw new ValidationException("Login HRD element must be the last login placeholder element.");
                        }
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError(nameof(Api.LoginUpParty.Elements).ToCamelCase(), vex.Message);
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
                    ValidateHtml(extendedUiElements);
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

        private static void ValidateHtml(List<Api.DynamicElement> dynamicElement)
        {
            foreach (var element in dynamicElement)
            {
                if (element.Type == Api.DynamicElementTypes.Html)
                {
                    if (!element.Content.IsNullOrWhiteSpace())
                    {
                        var sanitizedContent = SanitizeHtml(element.Content);
                        element.Content = sanitizedContent;
                    }
                }
            }
        }

        private static string SanitizeHtml(string html)
        { 
            if (html.IsNullOrWhiteSpace())
            {
                return html;
            }

            var sanitized = html;
            var elementOptions = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
            var attributeOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

            // Remove disallowed tags entirely (both paired and self-closing)
            var disallowedTags = new[] { "script", "iframe", "object", "embed", "link", "meta", "form", "input", "button", "style" };
            foreach (var tag in disallowedTags)
            {
                sanitized = Regex.Replace(sanitized, $"<\\s*{tag}\\b[^>]*>.*?<\\s*/\\s*{tag}\\s*>", string.Empty, elementOptions);
                sanitized = Regex.Replace(sanitized, $"<\\s*{tag}\\b[^>]*/?\\s*>", string.Empty, elementOptions);
            }

            const string attributeValuePattern = "(?:\"[^\"]*\"|'[^']*'|[^\\s>]+)";

            // Remove inline event handlers (onload, onclick, ...)
            sanitized = Regex.Replace(sanitized, $"\\s+on[\\w-]+\\s*=\\s*{attributeValuePattern}", string.Empty, attributeOptions);

            // Remove inline styles entirely (CSS will be handled separately)
            sanitized = Regex.Replace(sanitized, $"\\s+style\\s*=\\s*{attributeValuePattern}", string.Empty, attributeOptions);

            // Remove attributes that can inject HTML content directly
            sanitized = Regex.Replace(sanitized, $"\\s+srcdoc\\s*=\\s*{attributeValuePattern}", string.Empty, attributeOptions);

            var protocolSensitiveAttributes = new[] { "href", "src", "xlink:href", "formaction", "action", "srcset" };
            foreach (var attribute in protocolSensitiveAttributes)
            {
                sanitized = Regex.Replace(sanitized, $"\\s+{attribute}\\s*=\\s*\"\\s*(?:javascript|vbscript)\\s*:[^\"]*\"", string.Empty, attributeOptions);
                sanitized = Regex.Replace(sanitized, $"\\s+{attribute}\\s*=\\s*'\\s*(?:javascript|vbscript)\\s*:[^']*'", string.Empty, attributeOptions);
                sanitized = Regex.Replace(sanitized, $"\\s+{attribute}\\s*=\\s*(?:javascript|vbscript)\\s*:[^\\s>]+", string.Empty, attributeOptions);
            }

            return sanitized.Trim();
        }
    }
}