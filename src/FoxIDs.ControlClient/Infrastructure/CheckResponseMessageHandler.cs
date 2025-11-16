using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Infrastructure;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Client.Infrastructure
{
    public class CheckResponseMessageHandler : DelegatingHandler
    {
        private readonly OpenidConnectPkce openidConnectPkce;

        public CheckResponseMessageHandler(OpenidConnectPkce openidConnectPkce)
        {
            this.openidConnectPkce = openidConnectPkce;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if(!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await (openidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();                    
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new FoxIDsApiException("Forbidden, you do not possess the required scope and role.", response.StatusCode, await GetResponseTextAsync(response), GetHeaders(response));
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new FoxIDsApiException("Bad request", response.StatusCode, await GetResponseTextAsync(response), GetHeaders(response));
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    throw new FoxIDsApiException("Internal server error", response.StatusCode, await GetResponseTextAsync(response), GetHeaders(response));
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new FoxIDsApiException("Conflict", response.StatusCode, await GetResponseTextAsync(response), GetHeaders(response));
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FoxIDsApiException("Not found", response.StatusCode, await GetResponseTextAsync(response), GetHeaders(response));
                }
                else if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new FoxIDsApiException($"The HTTP status code of the response was not expected.", response.StatusCode, await GetResponseTextAsync(response), GetHeaders(response));
                }
            }

            return response;
        }

        private IReadOnlyDictionary<string, IEnumerable<string>> GetHeaders(HttpResponseMessage response)
        {
            var headers = Enumerable.ToDictionary(response.Headers, h => h.Key, h => h.Value);
            if (response.Content != null && response.Content.Headers != null)
            {
                foreach (var item in response.Content.Headers)
                {
                    headers[item.Key] = item.Value;
                }
            }
            return headers;
        }

        private async Task<string> GetResponseTextAsync(HttpResponseMessage response)
        {
            if(response.Content == null)
            {
                return null;
            }
            else
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }
    }
}
