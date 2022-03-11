using Microsoft.AspNetCore.Mvc;
using System;

namespace FoxIDs
{
    public static class ActionResultExtensions
    {
        public static bool IsHtmlContent(this IActionResult result, Type notViewModelType = null)
        {
            if (result is ViewResult viewResult)
            {
                if (notViewModelType != null && viewResult.Model != null && viewResult.Model.GetType() == notViewModelType)
                {
                    return false;
                }
                return true;
            }
            else if (result is ContentResult)
            {
                if ("text/html".Equals((result as ContentResult).ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
