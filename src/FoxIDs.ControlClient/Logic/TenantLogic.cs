using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using FoxIDs.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TenantLogic
    {
        private const string ApiUri = "api/master/master/!tenant"; // "api/@master/!tenant";
        private readonly HttpClient httpClient;

        public TenantLogic(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task CreateTenantAsync(CreateTenantViewModel tenant)
        {
            try
            {
                using var response = await httpClient.PostAsJsonAsync(ApiUri, new Tenant { Name = tenant.Name });
                var tenantResponse = await response.ToObjectAsync<Tenant>();

            }
            catch (FoxIDsApiException ex)
            {

                throw;
            }

        }
    }
}
