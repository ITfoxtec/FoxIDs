using ITfoxtec.Identity;
using System;
using System.Collections.Generic;

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

        public static string PartyIdToName(this string upPartyId)
        {
            if (upPartyId.IsNullOrEmpty())
            {
                return upPartyId;
            }

            var split = upPartyId.Split(':');
            return split[split.Length - 1];
        }
    }
}
