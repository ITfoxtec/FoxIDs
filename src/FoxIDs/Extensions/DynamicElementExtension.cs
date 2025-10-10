using ITfoxtec.Identity;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace FoxIDs
{
    public static class DynamicElementExtension
    {
        private static readonly Regex removeHtmlCommentsRegex = new Regex("<!--.*-->", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex htmlHrefRegex = new Regex("href\\s*=\\s*(\"([^\"]*)\"|'([^']*)')", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IHtmlContent GetEmailControl(this IHtmlHelper html, string name, string value, int maxLength = Constants.Models.User.EmailLength, bool isRequired = false)
        {
            return html.GetControl("email", name, html.GetLocalizerValue("Email"), value, maxLength, validation: $"data-val-email=\"{html.GetErrorAttributeLocalizerMessage<EmailAddressAttribute>()}\"", autocomplete: "email", isRequired: isRequired);
        }

        public static IHtmlContent GetPhoneControl(this IHtmlHelper html, string name, string value, int maxLength = Constants.Models.User.PhoneLength, bool isRequired = false)
        {
            return html.GetControl("phone", name, html.GetLocalizerValue("Phone"), value, maxLength, validation: $"data-val-phone=\"{html.GetErrorAttributeLocalizerMessage<PhoneAttribute>()}\"", autocomplete: "tel", isRequired: isRequired);
        }

        public static IHtmlContent GetUsernameControl(this IHtmlHelper html, string name, string value, int maxLength = Constants.Models.User.UsernameLength, bool isRequired = false)
        {
            return html.GetControl("username", name, html.GetLocalizerValue("Username"), value, maxLength, autocomplete: "username", isRequired: isRequired);
        }

        public static IHtmlContent GetPasswordControl(this IHtmlHelper html, string name, string value, int maxLength = Constants.Models.Track.PasswordLengthMax, bool isRequired = false)
        {
            return html.GetControl("password", name, html.GetLocalizerValue("Password"), value, maxLength, autocomplete: "new-password", isRequired: isRequired);
        }

        public static IHtmlContent GetConfirmPasswordControl(this IHtmlHelper html, string name, string value, string matchPasswordName, int maxLength = Constants.Models.Track.PasswordLengthMax, bool isRequired = false)
        {
            var displayName = html.GetLocalizerValue("Confirm password");
            return html.GetControl("password", name, displayName, value, maxLength, validation: $"data-val-equalto=\"{html.GetLocalizerValue("'{0}' and 'Password' do not match.", displayName)}\" data-val-equalto-other=\"{matchPasswordName}\"", autocomplete: "new-password", isRequired: isRequired);
        }

        public static IHtmlContent GetNameControl(this IHtmlHelper html, string name, string value, int maxLength = 150, bool isRequired = false)
        {
            return html.GetControl("text", name, html.GetLocalizerValue("Full name"), value, maxLength, autocomplete: "name", isRequired: isRequired);
        }

        public static IHtmlContent GetGivenNameControl(this IHtmlHelper html, string name, string value, int maxLength = 80, bool isRequired = false)
        {
            return html.GetControl("text", name, html.GetLocalizerValue("Given name"), value, maxLength, autocomplete: "given-name", isRequired: isRequired);
        }

        public static IHtmlContent GetFamilyNameControl(this IHtmlHelper html, string name, string value, int maxLength = 80, bool isRequired = false)
        {
            return html.GetControl("text", name, html.GetLocalizerValue("Family name"), value, maxLength, autocomplete: "family-name", isRequired: isRequired);
        }

        public static IHtmlContent GetCustomControl(this IHtmlHelper html, string name, string value, string displayName, int maxLength, string regEx, string errorMessage, bool isRequired)
        {
            var validation = regEx.IsNullOrWhiteSpace() || errorMessage.IsNullOrWhiteSpace() ? null : $"data-val-regex=\"{html.GetLocalizerValue(errorMessage)}\" data-val-regex-pattern=\"{regEx}\"";
            return html.GetControl("text", name, html.GetLocalizerValue(displayName), value, maxLength, validation: validation, isRequired: isRequired);
        }

        public static IHtmlContent GetTextControl(this IHtmlHelper html, string content, IStringLocalizer stringLocalizer)
        {
            return html.GetContentControl(content, stringLocalizer, false);
        }
        public static IHtmlContent GetHtmlControl(this IHtmlHelper html, string content, IStringLocalizer stringLocalizer)
        {
            return html.GetContentControl(content, stringLocalizer, true);
        }

        private static IHtmlContent GetControl(this IHtmlHelper html, string type, string name, string displayName, string value, int maxLength, string validation = null, string autocomplete = null, bool isRequired = false)
        {
            (var hasError, var errorMessage) = GetError(html, name);

            var id = name.Replace('.', '_').Replace('[', '_').Replace(']', '_');
            var contentBuilder = new HtmlContentBuilder();
            contentBuilder.AppendHtml($"<input autocomplete=\"{(autocomplete.IsNullOrEmpty() ? "off" : autocomplete)}\" class=\"form-control input-control{(hasError ? " input-validation-error" : string.Empty)}\" autofocus=\"\" type=\"{type}\" data-val=\"true\"{(validation.IsNullOrEmpty() ? string.Empty : $" {validation}")} data-val-maxlength=\"{html.GetErrorAttributeLocalizerMessage<MaxLengthAttribute>(displayName, maxLength)}\" data-val-maxLength-max=\"{maxLength}\"{(isRequired ? $" data-val-required=\"{html.GetErrorAttributeLocalizerMessage<RequiredAttribute>(displayName)}\"" : string.Empty)} id=\"{id}\" maxLength=\"{maxLength}\" name=\"{name}\" value=\"{value}\">");
            contentBuilder.AppendHtml($"<label class=\"label-control\" for=\"{name}\">{displayName}</label>");
            contentBuilder.AppendHtml($"<span class=\"{(hasError ? "field-validation-error" : "field-validation-valid")}\" data-valmsg-for=\"{name}\" data-valmsg-replace=\"true\">{errorMessage}</span>");
            return contentBuilder;
        }

        private static IHtmlContent GetContentControl(this IHtmlHelper html, string content, IStringLocalizer stringLocalizer, bool isHtml)
        {
            var contentBuilder = new HtmlContentBuilder();
            if (isHtml)
            {
                var cleanedContent = RemoveHtmlComments(content);
                var contentWithoutHrefValues = ReplaceHrefValuesWithPlaceholders(cleanedContent, out var hrefValues);
                if (hrefValues.Length > 0)
                {
                    contentBuilder.AppendHtml(stringLocalizer.GetString(contentWithoutHrefValues, hrefValues));
                }
                else
                {
                    contentBuilder.AppendHtml(stringLocalizer.GetString(contentWithoutHrefValues));
                }
            }
            else
            {
                contentBuilder.Append(stringLocalizer.GetString(content));
            }
            return contentBuilder;
        }

        private static string ReplaceHrefValuesWithPlaceholders(string html, out object[] hrefValues)
        {
            if (html.IsNullOrWhiteSpace())
            {
                hrefValues = Array.Empty<object>();
                return html;
            }

            var urls = new List<object>();

            var content = htmlHrefRegex.Replace(html, match =>
            {
                var url = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                var placeholder = $"{{{urls.Count}}}";
                urls.Add(url);
                return $"href=\"{placeholder}\"";
            });

            hrefValues = urls.ToArray();
            return content;
        }

        private static string RemoveHtmlComments(string html)
        {
            if (html.IsNullOrWhiteSpace())
            {
                return html;
            }

            return removeHtmlCommentsRegex.Replace(html, string.Empty).Trim();
        }

        private static (bool hasError, string errorMessage) GetError(IHtmlHelper html, string name)
        {
            if (!html.ViewData.ModelState.IsValid && html.ViewData.ModelState[name]?.ValidationState == ModelValidationState.Invalid)
            {
                var error = html.ViewData.ModelState[name].Errors.FirstOrDefault();
                if (error != null && !error.ErrorMessage.IsNullOrEmpty())
                {
                    return (true, error.ErrorMessage);
                }
            }

            return (false, string.Empty);
        }

        private static string GetLocalizerValue(this IHtmlHelper html, string name)
        {
            return html.ViewContext.HttpContext.RequestServices.GetService<IStringLocalizer>()[name];
        }
        private static string GetLocalizerValue(this IHtmlHelper html, string name, params object[] arguments)
        {
            return html.ViewContext.HttpContext.RequestServices.GetService<IStringLocalizer>()[name, arguments];
        }
        private static string GetErrorAttributeLocalizerMessage<T>(this IHtmlHelper html) where T : ValidationAttribute, new()
        {
            return html.ViewContext.HttpContext.RequestServices.GetService<IStringLocalizer>()[GetErrorAttributeMessage<T>()];
        }
        private static string GetErrorAttributeLocalizerMessage<T>(this IHtmlHelper html, params object[] arguments) where T : ValidationAttribute, new()
        {
            return html.ViewContext.HttpContext.RequestServices.GetService<IStringLocalizer>()[GetErrorAttributeMessage<T>(), arguments];
        }

        private static string GetErrorAttributeMessage<T>() where T : ValidationAttribute, new() 
        {
            var attribute = new T();
            if(attribute is MaxLengthAttribute)
            {
                return attribute.FormatErrorMessage("{0}").Replace("-1", "{1}");
            }
            else
            {
                return attribute.FormatErrorMessage("{0}");
            }
        }
    }
}
