using ITfoxtec.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for HTML form and redirect actions.
    /// </summary>
    public static class HtmActionExtensions
    {
        private const string defaultTitle = "FoxIDs";

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a HTML Post ContentResult.
        /// </summary>
        public static Task<ContentResult> ToHtmlPostContentResultAsync(this Dictionary<string, string> items, string url, string title)
        {
            return items.ToHtmlPostPage(url, title: title ?? defaultTitle).ToContentResultAsync();
        }

        /// <summary>
        /// Converts a URL to a redirect ContentResult.
        /// </summary>
        public static ContentResult ToRedirectResult(this string url, string title)
        {
            return url.HtmRedirectActionPage(title: title ?? defaultTitle).ToContentResult();
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a redirect ContentResult.
        /// </summary>
        public static Task<ContentResult> ToRedirectResultAsync(this Dictionary<string, string> items, string url, string title)
        {
            return items.ToHtmlGetPage(url, title: title ?? defaultTitle).ToContentResultAsync();
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a fragment ContentResult.
        /// </summary>
        public static Task<ContentResult> ToFragmentResultAsync(this Dictionary<string, string> items, string url, string title)
        {
            return items.ToHtmlFragmentPage(url, title: title ?? defaultTitle).ToContentResultAsync();
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

        /// <summary>
        /// HTML to ContentResult.
        /// </summary>
        public static Task<ContentResult> ToContentResultAsync(this string html)
        {
            return Task.FromResult(html.ToContentResult());
        }
    }
}
