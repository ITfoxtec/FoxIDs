using Microsoft.AspNetCore.Http;

namespace FoxIDs
{
    public static class HttpResponseExtensions
    {
        public static void SetHeader(this HttpResponse response, string key, string value)
        {
            response.RemoveHeader(key);
            response.Headers.Add(key, value);
        }

        public static void RemoveHeader(this HttpResponse response, string key)
        {
            if (response.Headers.ContainsKey(key))
            {
                response.Headers.Remove(key);
            }
        }
    }
}
