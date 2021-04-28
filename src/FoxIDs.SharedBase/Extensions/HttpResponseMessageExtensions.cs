using FoxIDs.Infrastructure;
using ITfoxtec.Identity;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FoxIDs
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
            if(response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception($"Error, Bad request. StatusCode={response.StatusCode}");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            if(responseText.IsNullOrWhiteSpace())
            {
                return default;
            }

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
