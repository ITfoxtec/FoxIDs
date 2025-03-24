using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class SmsPriceService : BaseService
    {
        private const string apiUri = "api/@master/!smsPrice";
        private const string listApiUri = "api/@master/!smsPrices";

        public SmsPriceService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<SmsPrice>> GetSmsPricesAsync(string filterName, string paginationToken = null) => await GetListAsync<SmsPrice>(listApiUri, filterName, paginationToken: paginationToken);

        public async Task<SmsPrice> GetSmsPriceAsync(string iso2) => await GetAsync<SmsPrice>(apiUri, iso2, parmName1: nameof(iso2));
        public async Task CreateSmsPriceAsync(SmsPrice smsPrice) => await PostResponseAsync<SmsPrice, SmsPrice>(apiUri, smsPrice);
        public async Task UpdateSmsPriceAsync(SmsPrice smsPrice) => await PutResponseAsync<SmsPrice, SmsPrice>(apiUri, smsPrice);
        public async Task DeleteSmsPriceAsync(string iso2) => await DeleteAsync(apiUri, iso2, parmName1: nameof(iso2));
    }
}
