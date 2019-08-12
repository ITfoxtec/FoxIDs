using ITfoxtec.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.SeedTool
{
    public static class HttpClientExtensions
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public static Task<HttpResponseMessage> GetJsonAsync(this HttpClient client, string requestUri, params object[] data)
        {
            var nameValueCollection = new Dictionary<string, string>();
            if (data != null && data.Count() > 0)
            {

                nameValueCollection = data[0].ToDictionary();
                if (data.Count() > 1)
                {
                    foreach (var item in data.Skip(1).Where(d => d != null))
                    {
                        nameValueCollection = nameValueCollection.AddToDictionary(item);
                    }
                }
            }
            return client.GetAsync(QueryHelpers.AddQueryString(requestUri, nameValueCollection));
        }

        public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string requestUri, object data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = new StringContent(JsonConvert.SerializeObject(data, Settings), Encoding.UTF8, "application/json");
            return client.SendAsync(request);
        }

        public static Task<HttpResponseMessage> UpdateJsonAsync(this HttpClient client, string requestUri, object data)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
            request.Content = new StringContent(JsonConvert.SerializeObject(data, Settings), Encoding.UTF8, "application/json");
            return client.SendAsync(request);
        }
        public static Task<HttpResponseMessage> PatchJsonAsync(this HttpClient client, string requestUri, object data)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri);
            request.Content = new StringContent(JsonConvert.SerializeObject(data, Settings), Encoding.UTF8, "application/json");
            return client.SendAsync(request);
        }

        public static Task<HttpResponseMessage> DeleteJsonAsync(this HttpClient client, string requestUri, object data)
        {
            var nameValueCollection = data.ToDictionary();
            return client.DeleteAsync(QueryHelpers.AddQueryString(requestUri, nameValueCollection));
        }
    }
}
