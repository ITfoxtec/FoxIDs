using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class SmsPriceService : BaseService
    {
        private const string apiUri = "api/@master/!smsPrice";
        private const string listApiUri = "api/@master/!smsPrices";

        public SmsPriceService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<SmsPrice>> GetSmsPricesAsync(string filterName, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<SmsPrice>(listApiUri, filterName, paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<SmsPrice> GetSmsPriceAsync(string iso2, CancellationToken cancellationToken = default) => await GetAsync<SmsPrice>(apiUri, iso2, parmName1: nameof(iso2), cancellationToken: cancellationToken);
        public async Task CreateSmsPriceAsync(SmsPrice smsPrice, CancellationToken cancellationToken = default) => await PostResponseAsync<SmsPrice, SmsPrice>(apiUri, smsPrice, cancellationToken);
        public async Task UpdateSmsPriceAsync(SmsPrice smsPrice, CancellationToken cancellationToken = default) => await PutResponseAsync<SmsPrice, SmsPrice>(apiUri, smsPrice, cancellationToken);
        public async Task DeleteSmsPriceAsync(string iso2, CancellationToken cancellationToken = default) => await DeleteAsync(apiUri, iso2, parmName1: nameof(iso2), cancellationToken: cancellationToken);
    }
}
