using Amazon.Runtime;
using AutoMapper;
using FoxIDs.Client.Pages.Components;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using FoxIDs.Util;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using ExtInv = FoxIDs.Models.ExternalInvoice;

namespace FoxIDs.Logic.Usage
{
    public class InvoiceLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryLogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;
        private readonly ITenantDataRepository tenantDataRepository;

        public InvoiceLogic(FoxIDsControlSettings settings, TelemetryLogger logger, IHttpClientFactory httpClientFactory, IMapper mapper, IMasterDataRepository masterDataRepository, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
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
            var invoice = await CreateInvoiceAsync(used);

            _ = await MakeInvoiceRequest(invoice, used, false);

            if (used.Invoices?.Count > 0)
            {
                used.Invoices.Add(invoice);
            }
            else 
            {
                used.Invoices = [invoice];
            }

        }

        private async Task<Invoice> CreateInvoiceAsync(Used used)
        {
            var tenant = await tenantDataRepository.GetTenantByNameAsync(RouteBinding.TenantName);
            if (tenant.PlanName.IsNullOrWhiteSpace())
            {
                throw new Exception($"Tenant '{RouteBinding.TenantName}' do not have a plan.");
            }
            var plan = await masterDataRepository.GetAsync<Plan>(await Plan.IdFormatAsync(tenant.PlanName));

            if (plan.CostPerMonth <= 0)
            {
                throw new Exception($"Tenant '{RouteBinding.TenantName}' plan '{tenant.PlanName}' do not have a cost per month.");
            }

            var invoice = new Invoice
            {
                InvoiceNumber = GetNextInvoiceNumber(),
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Customer = tenant.Customer,
            };

            var trackPrice = GetTrackPrice(used, plan);

            invoice.Price = plan.CostPerMonth + trackPrice;
            invoice.Vat = 0;
            invoice.TotalPrice = invoice.Price + invoice.Vat;

            await invoice.ValidateObjectAsync();
            return invoice;
        }

        private string GetNextInvoiceNumber()
        {
            return "fc-00003";
        }

        private decimal GetTrackPrice(Used used, Plan plan)
        {
            return (decimal)used.Tracks * plan.Tracks.FirstLevelCost;
        }

        private async Task<ExtInv.InvoiceResponse> MakeInvoiceRequest(Invoice invoice, Used used, bool sendInvoice)
        {
            var invoiceRequest = mapper.Map<ExtInv.InvoiceRequest>(invoice);
            invoiceRequest.SendInvoice = sendInvoice;
            invoiceRequest.CardPayment = true;
            invoiceRequest.PeriodYear = used.PeriodYear;
            invoiceRequest.PeriodMonth = used.PeriodMonth;
            await invoiceRequest.ValidateObjectAsync();

            var requestDictionary = invoiceRequest.ToDictionary();
            var httpClient = httpClientFactory.CreateClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{settings.Usage.ExternalInvoiceApiId.OAuthUrlDencode()}:{settings.Usage.ExternalInvoiceApiSecret.OAuthUrlDencode()}".Base64Encode());
            var content = new StringContent(JsonConvert.SerializeObject(requestDictionary, JsonSettings.ExternalSerializerSettings), Encoding.UTF8, MediaTypeNames.Application.Json);
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
