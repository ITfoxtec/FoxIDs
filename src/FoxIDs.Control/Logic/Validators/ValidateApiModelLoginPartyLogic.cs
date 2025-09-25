using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity;
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
        private static readonly Regex cssUnsafePattern = new Regex("(?i)(expression\\s*\\(|behavior(u)?r\\s*:|-moz-binding\\s*:|@import\\b|@charset\\b|@namespace\\b|url\\s*\\(\\s*[\'\\\"]?\\s*(?:javascript|vbscript|data)\\s*:|<\\s*/?\\s*(?:style|script)[^>]*>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

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

            if (!TryValidateIconUrl(modelState, logger, nameof(Api.LoginUpParty.IconUrl), party.IconUrl))
            {
                isValid = false;
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

        internal static bool TryValidateIconUrl(ModelStateDictionary modelState, TelemetryScopedLogger logger, string iconUrlFieldName, string iconUrl)
        {
            if (iconUrl.IsNullOrWhiteSpace())
            {
                return true;
            }

            try
            {
                var iconExtension = Path.GetExtension(iconUrl.Split('?')[0]);
                _ = iconExtension switch
                {
                    ".ico" => "image/x-icon",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    _ => throw new ValidationException($"Icon image format '{iconExtension}' not supported.")
                };

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
    }
}
