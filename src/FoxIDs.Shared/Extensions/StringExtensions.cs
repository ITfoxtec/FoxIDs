using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string text)
        {
            if(text.IsNullOrEmpty())
            {
                return text;
            }

            var resultText = new List<string>();

            var textSplit = text.Split('.');
            foreach (var item in textSplit)
            {
                if(item.Length > 0)
                {
                    resultText.Add($"{Char.ToLowerInvariant(item[0])}{(item.Length > 1 ? item.Substring(1) : string.Empty)}");
                }
                else
                {
                    resultText.Add(item);
                }
            }

            return string.Join('.', resultText);
        }

        public static string UrlToDomain(this string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            var splitValue = url.Split('/');
            if (splitValue.Count() > 2)
            {
                var domain = splitValue[2].ToLower();
                return domain;
            }
            return null;
        }

        public static string DomainToOrigin(this string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return null;
            }

            return $"https://{domain}";
        }

        public static string UrlToOrigin(this string url)
        {
            var domain = url.UrlToDomain();
            return domain.DomainToOrigin();
        }
    }
}
