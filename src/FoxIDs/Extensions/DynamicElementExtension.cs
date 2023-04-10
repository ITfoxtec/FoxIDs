using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using ITfoxtec.Identity;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using Microsoft.IdentityModel.Abstractions;

namespace FoxIDs
{
    public static class DynamicElementExtension
    {
        public static IHtmlContent GetEmailControl(this IHtmlHelper html, string name, string value, int maxlength = 60, bool isRequired = false)
        {
            return html.GetControl("email", name, html.GetLocalizerValue("Email"), value, maxlength, validation: $"data-val-email=\"{html.GetErrorAttributeLocalizerMessage<EmailAddressAttribute>()}\"", autocomplete: "username", isRequired: isRequired);
        }

        public static IHtmlContent GetPasswordControl(this IHtmlHelper html, string name, string value, int maxlength = 50, bool isRequired = false)
        {
            return html.GetControl("password", name, html.GetLocalizerValue("Password"), value, maxlength, autocomplete: "new-password", isRequired: isRequired);
        }

        public static IHtmlContent GetConfirmPasswordControl(this IHtmlHelper html, string name, string value, string matchPasswordName, int maxlength = 50, bool isRequired = false)
        {
            var displayName = html.GetLocalizerValue("Confirm password");
            return html.GetControl("password", name, displayName, value, maxlength, validation: $"data-val-equalto=\"{html.GetLocalizerValue("'{0}' and 'Password' do not match.", displayName)}\" data-val-equalto-other=\"{matchPasswordName}\"", autocomplete: "new-password", isRequired: isRequired);
        }

        public static IHtmlContent GetNameControl(this IHtmlHelper html, string name, string value, int maxlength = 150, bool isRequired = false)
        {
            return html.GetControl("text", name, html.GetLocalizerValue("Full name"), value, maxlength, autocomplete: "name", isRequired: isRequired);
        }

        public static IHtmlContent GetGivenNameControl(this IHtmlHelper html, string name, string value, int maxlength = 80, bool isRequired = false)
        {
            return html.GetControl("text", name, html.GetLocalizerValue("Given name"), value, maxlength, autocomplete: "given-name", isRequired: isRequired);
        }

        public static IHtmlContent GetFamilyNameControl(this IHtmlHelper html, string name, string value, int maxlength = 80, bool isRequired = false)
        {
            return html.GetControl("text", name, html.GetLocalizerValue("Family name"), value, maxlength, autocomplete: "family-name", isRequired: isRequired);
        }

        private static IHtmlContent GetControl(this IHtmlHelper html, string type, string name, string displayName, string value, int maxlength, string validation = null, string autocomplete = null, bool isRequired = false)
        {
            (var hasError, var errorMessage) = GetError(html, name);

            var id = name.Replace('.', '_').Replace('[', '_').Replace(']', '_');
            var content = new HtmlContentBuilder();
            content.AppendHtml($"<input{(autocomplete.IsNullOrEmpty() ? string.Empty : $" autocomplete=\"{autocomplete}\"")} class=\"form-control input-control{(hasError ? " input-validation-error" : string.Empty)}\" autofocus=\"\" type=\"{type}\" data-val=\"true\"{(validation.IsNullOrEmpty() ? string.Empty : $" {validation}")} data-val-maxlength=\"{html.GetErrorAttributeLocalizerMessage<MaxLengthAttribute>(displayName, maxlength)}\" data-val-maxlength-max=\"{maxlength}\"{(isRequired ? $" data-val-required=\"{html.GetErrorAttributeLocalizerMessage<RequiredAttribute>(displayName)}\"" : string.Empty)} id=\"{id}\" maxlength=\"{maxlength}\" name=\"{name}\" value=\"{value}\">");
            content.AppendHtml($"<label class=\"label-control\" for=\"{name}\">{displayName}</label>");
            content.AppendHtml($"<span class=\"{(hasError ? "field-validation-error" : "field-validation-valid")}\" data-valmsg-for=\"{name}\" data-valmsg-replace=\"true\">{errorMessage}</span>");
            return content;
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
