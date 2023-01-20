using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace FoxIDs
{
    public static class HttpContextExtensions
    {
        public static CultureInfo GetCulture(this HttpContext context)
        {
            return context.Features.Get<IRequestCultureFeature>()?.RequestCulture?.UICulture ?? new CultureInfo("en");
        }

        public static string GetCultureParentName(this HttpContext context)
        {
            var culture = context.GetCulture();
            if (culture.Parent != null && !culture.Parent.Name.IsNullOrWhiteSpace())
            {
                return culture.Parent.Name.ToLower();
            }
            else
            {
                return culture.Name.ToLower();
            }
        }
    }
}
