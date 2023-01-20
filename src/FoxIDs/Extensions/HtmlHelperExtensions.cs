using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoxIDs
{
    public static class HtmlHelperExtensions
    {
        public static string ParentCultureName<T>(this IHtmlHelper<T> htmlHelper)
        {
            return htmlHelper.ViewContext.HttpContext.GetCultureParentName();
        }
    }
}
