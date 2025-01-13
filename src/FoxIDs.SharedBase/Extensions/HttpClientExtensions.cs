using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs
{
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PostAsFoxIDsApiJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value)
        {
            return client.PostAsJsonAsync(requestUri, value, JsonFoxIDsApiSerializerExtensions.JsonSerializerOptions);
        }

        public static Task<HttpResponseMessage> PutAsFoxIDsApiJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value)
        {
            return client.PutAsJsonAsync(requestUri, value, JsonFoxIDsApiSerializerExtensions.JsonSerializerOptions);
        }

        /// <summary>
        /// Plain JSON POST without "transfer-encoding": "chunked". Used for interoperability.
        /// </summary>
        public static Task<HttpResponseMessage> PostAsPlainJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value)
        {
            return client.PostAsync(requestUri, GetJsonAsStringContent(value));
        }

        /// <summary>
        /// Plain JSON PUT without "transfer-encoding": "chunked". Used for interoperability.
        /// </summary>
        public static Task<HttpResponseMessage> PutAsPlainJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value)
        {
            return client.PutAsync(requestUri, GetJsonAsStringContent(value));
        }

        private static HttpContent GetJsonAsStringContent<TValue>(TValue value)
        {
            var jsonString = value.JsonNewtonsoftSerialize();
            return new StringContent(jsonString, Encoding.UTF8, MediaTypeNames.Application.Json);
        } 
    }
}
