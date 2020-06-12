using ITfoxtec.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for ActionResult and dictionarys.
    /// </summary>
    public static class ListActionResultExtensions
    {
        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a HTML Post Content Result.
        /// </summary>
        public static Task<ContentResult> ToHtmlPostContentResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new ContentResult
            {
                ContentType = "text/html",
                Content = items.ToHtmlPostPage(url),
            });
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a Redirect Result.
        /// </summary>
        public static Task<RedirectResult> ToRedirectResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new RedirectResult(QueryHelpers.AddQueryString(url, items)));
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a Fragment Result.
        /// </summary>
        public static Task<RedirectResult> ToFragmentResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new RedirectResult(QueryHelpers.AddQueryString(url, items).Replace('?', '#')));
        }
    }
}
