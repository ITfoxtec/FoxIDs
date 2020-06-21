﻿using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class UpPartyService : BaseService
    {
        private const string loginApiUri = "api/{tenant}/master/!loginupparty";
        private const string filterApiUri = "api/{tenant}/master/!filterupparty";
        private readonly HttpClient httpClient;

        public UpPartyService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(routeBindingLogic)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<UpParty>> FilterUpPartyAsync(string filterName)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(filterApiUri)}?filterName={filterName}");
            var upParties = await response.ToObjectAsync<IEnumerable<UpParty>>();
            return upParties;
        }

        public async Task<LoginUpParty> GetLoginUpPartyAsync(string name)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(loginApiUri)}?name={name}");
            var loginUpParties = await response.ToObjectAsync<LoginUpParty>();
            return loginUpParties;
        }

        public async Task CreateLoginUpPartyAsync(LoginUpParty party)
        {
            using var response = await httpClient.PostAsJsonAsync(await GetTenantApiUrlAsync(loginApiUri), party);
        }

        public async Task UpdateLoginUpPartyAsync(LoginUpParty party)
        {
            using var response = await httpClient.PutAsJsonAsync(await GetTenantApiUrlAsync(loginApiUri), party);
        }

    }
}
