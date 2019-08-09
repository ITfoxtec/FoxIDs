using ITfoxtec.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoxIDs
{
    public static class HtmlHelperExtensions
    {
        public static string ParentCultureName<T>(this IHtmlHelper<T> htmlHelper)
        {
            var culture = htmlHelper.ViewContext.HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.UICulture;
            if(culture.Parent != null && !culture.Parent.Name.IsNullOrWhiteSpace())
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
