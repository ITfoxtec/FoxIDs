using FoxIDs.Client.Infrastructure;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FoxIDs.Client
{
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Converts a HttpResponseMessage json string to an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public static async Task<T> ToObjectAsync<T>(this HttpResponseMessage response)
        {
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                return responseText.JsonDeserialize<T>();
            }
            catch (JsonException ex)
            {
                throw new FoxIDsApiException($"Could not deserialize the response body string as '{typeof(T).Name} '.", response.StatusCode, responseText, ex);
            }
        }
    }
}
