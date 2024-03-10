using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;

namespace FoxIDs
{
    public static class StringExtensions
    {
        public static string GetCorsOriginFromUrl(this string url)
        {
            if (!url.IsNullOrEmpty())
            {
                var split = url.Split("://");
                if (split.Length == 2)
                {
                    return $"{split[0]}://{split[1].TrimEnd('/')}";
                }
            }
            return url;
        }
    }
}
