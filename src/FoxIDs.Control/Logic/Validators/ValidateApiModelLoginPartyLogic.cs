using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using FoxIDs.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace FoxIDs.Logic
{
    public class ValidateApiModelLoginPartyLogic : LogicBase
    {
        private static readonly Regex cssCommentPattern = new Regex(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private static readonly Regex cssHtmlElementPattern = new Regex("<[^>]+>.*?</[^>]+>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private static readonly Regex cssStyleTagPattern = new Regex("<\\s*/?\\s*style[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex cssHtmlTagPattern = new Regex("<[^>]+>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex cssHtmlCommentPattern = new Regex("<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private static readonly Regex cssUnsafePattern = new Regex("(?i)(expression\\s*\\(|behavior(u)?r\\s*:|-moz-binding\\s*:|@import\\b|@charset\\b|@namespace\\b|url\\s*\\(\\s*[\'\\\"]?\\s*(?:javascript|vbscript|data(?!\\s*:\\s*image\\/))\\s*:|<\\s*/?\\s*(?:style|script)[^>]*>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private const string DataUriPrefix = "data:";
        private static readonly IDictionary<string, string> iconExtensionToMimeType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".ico"] = "image/x-icon",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp"
        };
        private static readonly HashSet<string> supportedIconMimeTypes = new HashSet<string>(iconExtensionToMimeType.Values, StringComparer.OrdinalIgnoreCase);

        private readonly TelemetryScopedLogger logger;
        private readonly ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic;
        private readonly ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic;

        public ValidateApiModelLoginPartyLogic(TelemetryScopedLogger logger, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.validateApiModelGenericPartyLogic = validateApiModelGenericPartyLogic;
            this.validateApiModelDynamicElementLogic = validateApiModelDynamicElementLogic;
        }

        public async Task<bool> ValidateApiModelAsync(ModelStateDictionary modelState, Api.LoginUpParty party)
        {
            var isValid = true;

            if (!TryValidateAndSanitizeCss(modelState, logger, nameof(Api.LoginUpParty.Css), party.Css, out var sanitizedCss))
            {
                isValid = false;
            }
            else
            {
                party.Css = sanitizedCss;
            }

            if (!TryValidateIconUrl(modelState, logger, nameof(Api.LoginUpParty.IconUrl), party.IconUrl, out var sanitizedIconUrl))
            {
                isValid = false;
            }
            else
            {
                party.IconUrl = sanitizedIconUrl;
            }

            if (party.TwoFactorAppName.IsNullOrWhiteSpace())
            {
                party.TwoFactorAppName = RouteBinding.TenantName;
            }

            if (party.Elements?.Any() == true)
            {
                if (!validateApiModelDynamicElementLogic.ValidateApiModelLoginElements(modelState, party.Elements))
                {
                    isValid = false;
                }
            }
            else
            {
                party.Elements = null;
            }

            if (party.CreateUser != null)
            {
                if (!party.EnableCreateUser || party.CreateUser.Elements?.Any() != true)
                {
                    party.CreateUser = null;
                }
                else
                {
                    if (!validateApiModelDynamicElementLogic.ValidateApiModelCreateUserElements(modelState, party.CreateUser.Elements, party.DisablePasswordAuth == true, party.EnablePasswordlessEmail == true, party.EnablePasswordlessSms == true))
                    {
                        isValid = false;
                    }

                    if (!validateApiModelGenericPartyLogic.ValidateApiModelClaimTransforms(modelState, party.CreateUser.ClaimTransforms, errorFieldName: nameof(Api.LoginUpParty.CreateUser.ClaimTransforms)))
                    {
                        isValid = false;
                    }
                }
            }

            if (!validateApiModelGenericPartyLogic.ValidateExtendedUi(modelState, party.ExtendedUis))
            {
                isValid = false;
            }

            return await Task.FromResult(isValid);
        }

        internal static bool TryValidateAndSanitizeCss(ModelStateDictionary modelState, TelemetryScopedLogger logger, string cssFieldName, string css, out string sanitizedCss)
        {
            sanitizedCss = css;
            if (css.IsNullOrWhiteSpace())
            {
                return true;
            }

            try
            {
                if (!HasBalancedCssBraces(css))
                {
                    throw new ValidationException("CSS contains unbalanced braces.");
                }

                sanitizedCss = SanitizeCss(css);
                return true;
            }
            catch (ValidationException vex)
            {
                logger.Warning(vex);
                modelState.TryAddModelError(cssFieldName.ToCamelCase(), vex.Message);
                return false;
            }
        }

        internal static bool TryValidateIconUrl(ModelStateDictionary modelState, TelemetryScopedLogger logger, string iconUrlFieldName, string iconUrl, out string sanitizedIconUrl)
        {
            sanitizedIconUrl = iconUrl;
            if (iconUrl.IsNullOrWhiteSpace())
            {
                return true;
            }

            try
            {
                sanitizedIconUrl = iconUrl.Trim();
                sanitizedIconUrl = RemoveCssUrlWrapper(sanitizedIconUrl);
                if (IsDataImageIcon(sanitizedIconUrl))
                {
                    ValidateDataImageIcon(sanitizedIconUrl);
                }
                else
                {
                    ValidateIconUrlExtension(sanitizedIconUrl);
                }

                return true;
            }
            catch (ValidationException vex)
            {
                logger.Warning(vex);
                modelState.TryAddModelError(iconUrlFieldName.ToCamelCase(), vex.Message);
                return false;
            }
        }

        private static string SanitizeCss(string css)
        {
            if (css.IsNullOrWhiteSpace())
            {
                return css;
            }

            var sanitized = css;
            sanitized = RemoveUnsafeComments(sanitized);
            sanitized = cssHtmlCommentPattern.Replace(sanitized, string.Empty);
            sanitized = cssHtmlElementPattern.Replace(sanitized, string.Empty);
            sanitized = cssStyleTagPattern.Replace(sanitized, string.Empty);
            sanitized = cssHtmlTagPattern.Replace(sanitized, string.Empty);
            sanitized = cssUnsafePattern.Replace(sanitized, string.Empty);

            return sanitized.Trim();
        }

        private static bool HasBalancedCssBraces(string css)
        {
            var balance = 0;
            foreach (var ch in css)
            {
                if (ch == '{')
                {
                    balance++;
                }
                else if (ch == '}')
                {
                    balance--;
                    if (balance < 0)
                    {
                        return false;
                    }
                }
            }

            return balance == 0;
        }

        private static string RemoveUnsafeComments(string css)
        {
            return cssCommentPattern.Replace(css, match => cssUnsafePattern.IsMatch(match.Value) ? string.Empty : match.Value);
        }

        private static string RemoveCssUrlWrapper(string iconUrl)
        {
            if (iconUrl.StartsWith("url(", StringComparison.OrdinalIgnoreCase) && iconUrl.EndsWith(")"))
            {
                var inner = iconUrl.Substring(4, iconUrl.Length - 5).Trim();
                inner = inner.Trim('\'', '"');
                return inner;
            }

            return iconUrl;
        }

        private static bool IsDataImageIcon(string iconUrl)
        {
            return iconUrl.StartsWith(DataUriPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateIconUrlExtension(string iconUrl)
        {
            var iconPath = iconUrl.Split(['?', '#'], 2)[0];
            var iconExtension = Path.GetExtension(iconPath);

            if (iconExtension.IsNullOrWhiteSpace() || !iconExtensionToMimeType.ContainsKey(iconExtension))
            {
                throw new ValidationException($"Icon image format '{iconExtension}' not supported.");
            }
        }

        private static void ValidateDataImageIcon(string iconUrl)
        {
            var commaIndex = iconUrl.IndexOf(',');
            if (commaIndex < 0)
            {
                throw new ValidationException("Icon data URI is missing image data.");
            }

            var metadata = iconUrl.Substring(DataUriPrefix.Length, commaIndex - DataUriPrefix.Length);
            if (metadata.IsNullOrWhiteSpace())
            {
                throw new ValidationException("Icon data URI is missing the media type.");
            }

            var metadataParts = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var mimeType = metadataParts.FirstOrDefault()?.Trim();
            if (mimeType.IsNullOrWhiteSpace())
            {
                throw new ValidationException("Icon data URI is missing the media type.");
            }

            if (!mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"Icon data URI media type '{mimeType}' is not an image.");
            }

            if (!supportedIconMimeTypes.Contains(mimeType))
            {
                throw new ValidationException($"Icon image format '{mimeType}' not supported.");
            }

            if (!metadataParts.Skip(1).Any(p => p.Equals("base64", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException("Icon data URI must specify base64 encoding.");
            }

            var imageData = iconUrl.Substring(commaIndex + 1).Trim();
            if (imageData.IsNullOrWhiteSpace())
            {
                throw new ValidationException("Icon data URI is missing image data.");
            }

            try
            {
                Convert.FromBase64String(imageData);
            }
            catch (FormatException)
            {
                throw new ValidationException("Icon data URI image data is not valid base64.");
            }
        }
    }
}

