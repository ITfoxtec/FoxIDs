using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs
{
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PostAsFoxIDsApiJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value, CancellationToken cancellationToken = default)
        {
            return client.PostAsJsonAsync(requestUri, value, JsonFoxIDsApiSerializerExtensions.JsonSerializerOptions, cancellationToken);
        }

        public static Task<HttpResponseMessage> PutAsFoxIDsApiJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value, CancellationToken cancellationToken = default)
        {
            return client.PutAsJsonAsync(requestUri, value, JsonFoxIDsApiSerializerExtensions.JsonSerializerOptions, cancellationToken);
        }

        /// <summary>
        /// Plain JSON POST without "transfer-encoding": "chunked". Used for interoperability.
        /// </summary>
        public static Task<HttpResponseMessage> PostAsPlainJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value, CancellationToken cancellationToken = default)
        {
            return client.PostAsync(requestUri, GetJsonAsStringContent(value), cancellationToken);
        }

        /// <summary>
        /// Plain JSON PUT without "transfer-encoding": "chunked". Used for interoperability.
        /// </summary>
        public static Task<HttpResponseMessage> PutAsPlainJsonAsync<TValue>(this HttpClient client, string requestUri, TValue value, CancellationToken cancellationToken = default)
        {
            return client.PutAsync(requestUri, GetJsonAsStringContent(value), cancellationToken);
        }

        private static HttpContent GetJsonAsStringContent<TValue>(TValue value)
        {
            var jsonString = value.JsonNewtonsoftSerialize();
            return new StringContent(jsonString, Encoding.UTF8, MediaTypeNames.Application.Json);
        } 
    }
}
