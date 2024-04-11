using System;
using System.Linq;

namespace FoxIDs
{
    public static class StringExtensions
    {
        public static string UrlToOrigin2(this string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            string[] splitScema = url.ToLower().Split("://");
            if (splitScema.Count() > 1)
            {
                string[] splitDomain = splitScema[1].Split('/');
                if (splitDomain.Count() >= 1)
                {
                    return $"{splitScema[0]}://{splitDomain[0]}";

                }
            }

            return null;
        }
    }
}
