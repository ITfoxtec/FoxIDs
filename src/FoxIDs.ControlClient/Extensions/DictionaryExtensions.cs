using System.Collections.Generic;

namespace FoxIDs.Client
{
    public static class DictionaryExtensions
    {
        public static string GetValue(this Dictionary<string, string> dictionary, string key)
        {
            if(dictionary != null && dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            return null;
        }
    }
}
