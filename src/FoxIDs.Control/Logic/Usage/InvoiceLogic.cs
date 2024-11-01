using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using FoxIDs.Util;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using ExtInv = FoxIDs.Models.ExternalInvoices;

namespace FoxIDs.Logic.Usage
{
    public class InvoiceLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;
        private readonly ITenantDataRepository tenantDataRepository;

        public InvoiceLogic(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, IMapper mapper, IMasterDataRepository masterDataRepository, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task CreateAndSendInvoiceAsync(Used used)
        {
            logger.Event($"Start tenant '{used.TenantName}' create and send invoice.");

            var invoice = await CreateInvoiceAsync(used);

            logger.Event($"Tenant '{used.TenantName}' invoice created.");

            _ = await MakeInvoiceRequest(invoice, used, true);

            logger.Event($"Tenant '{used.TenantName}' invoice send.");

            if (used.Invoices?.Count > 0)
            {
                used.Invoices.Add(invoice);
            }
            else 
            {
                used.Invoices = [invoice];
            }
        }

        public async Task CreateAndSendCreditNoteAsync(Used used)
        {
            logger.Event($"Start tenant '{used.TenantName}' create and send credit note.");

            var invoice = used.Invoices.LastOrDefault();
            if(invoice == null || invoice.IsCreditNote)
            {
                throw new Exception("Invalid last invoice.");
            }

            logger.Event($"Tenant '{used.TenantName}' credit note created.");

            _ = await MakeInvoiceRequest(invoice, used, true);

            logger.Event($"Tenant '{used.TenantName}' credit note send.");

            invoice.IsCreditNote = true;
            used.Invoices.Add(invoice);
        }

        private async Task<Invoice> CreateInvoiceAsync(Used used)
        {
            var tenant = await tenantDataRepository.GetTenantByNameAsync(used.TenantName);
            if (tenant.PlanName.IsNullOrWhiteSpace())
            {
                throw new Exception($"Tenant '{RouteBinding.TenantName}' do not have a plan.");
            }
            var plan = await masterDataRepository.GetAsync<Plan>(await Plan.IdFormatAsync(tenant.PlanName));

            if (plan.CostPerMonth <= 0)
            {
                throw new Exception($"Tenant '{RouteBinding.TenantName}' plan '{tenant.PlanName}' do not have a cost per month.");
            }

            (var invoiceNumber, var masterUsage) = await GetMasterUsageAndInvoiceNumberAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Seller = mapper.Map<Seller>(settings.Usage.Seller),
                Customer = tenant.Customer,
                Currency = tenant.Currency.IsNullOrEmpty() ? Constants.Models.Currency.Eur : tenant.Currency
            };

            CalculateInvoice(invoice, used, plan, tenant.IncludeVat, GetExchangesRate(invoice.Currency, masterUsage.CurrencyExchanges));

            await invoice.ValidateObjectAsync();
            return invoice;
        }

        private async Task<(string invoiceNumber, MasterUsage masterUsage)> GetMasterUsageAndInvoiceNumberAsync()
        {
            var masterUsage = await masterDataRepository.GetAsync<MasterUsage>(await MasterUsage.IdFormatAsync(), required: false);
            if(masterUsage == null)
            {
                masterUsage = new MasterUsage
                {
                    Id = await MasterUsage.IdFormatAsync(),
                    InvoiceNumber = 1
                };
                await masterDataRepository.CreateAsync(masterUsage);
            }
            else
            {
                masterUsage.InvoiceNumber += 1;
                await masterDataRepository.UpdateAsync(masterUsage);
            }

            var fullInvoiceNumber = $"{(masterUsage.InvoiceNumberPrefix.IsNullOrWhiteSpace() ? string.Empty : $"{masterUsage.InvoiceNumberPrefix}-")}{masterUsage.InvoiceNumber}";
            return (fullInvoiceNumber, masterUsage);
        }

        private decimal GetExchangesRate(string currency, List<CurrencyExchange> currencyExchanges)
        {
            if (currency == Constants.Models.Currency.Eur)
            {
                return 1;
            }
            else
            {
                var currencyExchange = currencyExchanges?.Where(e => e.Currency == currency).FirstOrDefault();
                if (currencyExchange == null)
                {
                    throw new Exception($"Missing currency exchange for '{currency}'.");
                }

                return currencyExchange.Rate;
            }
        }

        private void CalculateInvoice(Invoice invoice, Used used, Plan plan, bool includeVat, decimal exchangeRate)
        {
            invoice.Price = plan.CostPerMonth;

            invoice.Lines = new List<InvoiceLine>();
            invoice.Price += AddInvoiceLine(invoice.Lines, $"Additional environments ({plan.Tracks.Included} included)", $"Additional environments (more then {plan.Tracks.FirstLevelThreshold})", used.Tracks, plan.Tracks, exchangeRate);
            invoice.Price += AddInvoiceLine(invoice.Lines, $"Additional users ({plan.Users.Included} included)", $"Additional users (more then {plan.Users.FirstLevelThreshold})", used.Users, plan.Users, exchangeRate);
            invoice.Price += AddInvoiceLine(invoice.Lines, $"Additional logins ({plan.Logins.Included} included)", $"Additional logins (more then {plan.Logins.FirstLevelThreshold})", used.Logins, plan.Logins, exchangeRate);
            invoice.Price += AddInvoiceLine(invoice.Lines, $"Additional token requests ({plan.TokenRequests.Included} included)", $"Additional token requests (more then {plan.TokenRequests.FirstLevelThreshold})", used.TokenRequests, plan.TokenRequests, exchangeRate);
            invoice.Price += AddInvoiceLine(invoice.Lines, $"Additional Control API reads ({plan.ControlApiGetRequests.Included} included)", $"Additional Control API reads (more then {plan.ControlApiGetRequests.FirstLevelThreshold})", used.ControlApiGets, plan.ControlApiGetRequests, exchangeRate);
            invoice.Price += AddInvoiceLine(invoice.Lines, $"Additional Control API updates ({plan.ControlApiUpdateRequests.Included} included)", $"Additional Control API updates (more then {plan.ControlApiUpdateRequests.FirstLevelThreshold})", used.ControlApiUpdates, plan.ControlApiUpdateRequests, exchangeRate);

            if (includeVat)
            {
                invoice.Vat = RoundPrice(invoice.Price * settings.Usage.VatPercent / 100);
            }
            invoice.TotalPrice = invoice.Price + invoice.Vat;
        }

        private decimal AddInvoiceLine(List<InvoiceLine> lines, string textFirstLevel, string textSecondLevel, decimal usedCount, PlanItem planItem, decimal exchangeRate)
        {
            decimal price = 0;

            var firstLevel = planItem.FirstLevelThreshold > 0 && usedCount > planItem.FirstLevelThreshold ? Convert.ToDecimal(planItem.FirstLevelThreshold) : usedCount;
            var firstLevelUnitPrice = CurrencyAndRoundPrice(planItem.FirstLevelCost, exchangeRate);
            var firstLevelQuantity = usedCount > planItem.Included ? firstLevel - planItem.Included : 0;
            var firstLevelPrice = RoundPrice(firstLevelUnitPrice * firstLevelQuantity);
            lines.Add(new InvoiceLine { Text = textFirstLevel, Quantity = firstLevelQuantity, UnitPrice = firstLevelUnitPrice, Price = firstLevelPrice });
            price += firstLevelPrice;

            if (planItem.FirstLevelThreshold > 0 && usedCount > planItem.FirstLevelThreshold)
            {
                var secondLevelUnitPrice = CurrencyAndRoundPrice(planItem.SecondLevelCost.Value, exchangeRate);
                var secundLevelPriceQuantity = usedCount - planItem.FirstLevelThreshold.Value;
                var secundLevelPrice = RoundPrice(secondLevelUnitPrice * secundLevelPriceQuantity);
                lines.Add(new InvoiceLine { Text = textSecondLevel, Quantity = secundLevelPriceQuantity, UnitPrice = secondLevelUnitPrice, Price = secundLevelPrice });
                price += secundLevelPrice;
            }

            return price;
        }

        private decimal CurrencyAndRoundPrice(decimal price, decimal exchangeRate)
        {
            return RoundPrice(price * exchangeRate);
        }

        private decimal RoundPrice(decimal price)
        {
            return decimal.Round(price, 2);
        }

        private async Task<ExtInv.InvoiceResponse> MakeInvoiceRequest(Invoice invoice, Used used, bool sendInvoice)
        {
            var invoiceRequest = mapper.Map<ExtInv.InvoiceRequest>(invoice);
            invoiceRequest.SendInvoice = sendInvoice;
            invoiceRequest.CardPayment = true;
            invoiceRequest.PeriodYear = used.PeriodYear;
            invoiceRequest.PeriodMonth = used.PeriodMonth;
            await invoiceRequest.ValidateObjectAsync();

            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{settings.Usage.ExternalInvoiceApiId.OAuthUrlDencode()}:{settings.Usage.ExternalInvoiceApiSecret.OAuthUrlDencode()}".Base64Encode());
            var content = new StringContent(JsonConvert.SerializeObject(invoiceRequest, JsonSettings.ExternalSerializerSettings), Encoding.UTF8, MediaTypeNames.Application.Json);
            using var response = await httpClient.PostAsync(settings.Usage.ExternalInvoiceApiUrl, content);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var invoiceResponse = result.ToObject<ExtInv.InvoiceResponse>();
                    return invoiceResponse;

                default:
                    var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send external invoice request, error '{resultUnexpectedStatus}'. Status code={response.StatusCode}.");
            }

        }
    }
}
