using ITfoxtec.Identity;
using Microsoft.AspNetCore.Mvc;
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
        public static Task<ContentResult> ToRedirectResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new ContentResult
            {
                ContentType = "text/html",
                Content = items.ToHtmlGetPage(url),
            });
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, string&gt; to a Fragment Result.
        /// </summary>
        public static Task<ContentResult> ToFragmentResultAsync(this Dictionary<string, string> items, string url)
        {
            return Task.FromResult(new ContentResult
            {
                ContentType = "text/html",
                Content = items.ToHtmlFragmentPage(url),
            });
        }
    }
}
