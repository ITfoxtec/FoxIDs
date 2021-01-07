using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FoxIDs.Client
{
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PostAsFormatJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value)
        {
            return client.PostAsJsonAsync(requestUri, value, JsonSerializerExtensions.JsonSerializerOptions);
        }

        public static Task<HttpResponseMessage> PutAsFormatJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value)
        {
            return client.PutAsJsonAsync(requestUri, value, JsonSerializerExtensions.JsonSerializerOptions);
        }
    }
}
