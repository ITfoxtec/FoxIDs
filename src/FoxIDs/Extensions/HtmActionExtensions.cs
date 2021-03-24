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
        /// Converts a url to a Redirect Result.
        /// </summary>
        public static Task<ContentResult> ToRedirectResultAsync(this string url)
        {
            return Task.FromResult(new ContentResult
            {
                ContentType = "text/html",
                Content = HtmRedirectActionPageList(url),
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

        private static string HtmRedirectActionPageList(string url)
        {
            return
$@"<!DOCTYPE html>
<html lang=""en"">
    <head>
        <meta charset=""utf-8"" />
        <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
        <meta http-equiv=""refresh"" content=""0;URL='{url}'"" />
        <title>OAuth 2.0</title>
    </head>
    <body>
        <p>
            Please press <a href=""{url}"">here</a> to continue the proceed.
        </p>
    </body>
</html>";
        }
    }
}
