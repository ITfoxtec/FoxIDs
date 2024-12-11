using ITfoxtec.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for HTML form and redirect actions.
    /// </summary>
    public static class HtmlActionExtensions
    {
        /// <summary>
        /// Converts URL and Dictionary&lt;string, string&gt; to a HTML Post ContentResult.
        /// </summary>
        public static ContentResult ToHtmlPostContentResult(this string url, Dictionary<string, string> items)
        {
            return url.ToHtmlPostPage(items).ToContentResult();
        }

        /// <summary>
        /// Converts a URL to a RedirectResult.
        /// </summary>
        public static IActionResult ToRedirectResult(this string url)
        {
            return new RedirectResult(url);
        }

        /// <summary>
        /// Converts URL and Dictionary&lt;string, string&gt; to a RedirectResult.
        /// </summary>
        public static IActionResult ToRedirectResult(this string url, Dictionary<string, string> items)
        {
            return new RedirectResult(QueryHelpers.AddQueryString(url, items));
        }

        /// <summary>
        /// Converts URL and Dictionary&lt;string, string&gt; to a RedirectResult.
        /// </summary>
        public static IActionResult ToFragmentResult(this string url, Dictionary<string, string> items)
        {
            return new RedirectResult(url.AddFragment(items));
        }

        /// <summary>
        /// HTML to ContentResult.
        /// </summary>
        public static ContentResult ToContentResult(this string html)
        {
            return new ContentResult
            {
                ContentType = "text/html",
                Content = html,
            };
        }
    }
}
