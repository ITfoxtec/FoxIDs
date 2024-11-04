using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using FoxIDs.Util;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExtInv = FoxIDs.Models.ExternalInvoices;
using Mollie.Api.Client.Abstract;

namespace FoxIDs.Logic.Usage
{
    public class UsageInvoicingLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly IPaymentClient paymentClient;

        public UsageInvoicingLogic(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, IMapper mapper, IMasterDataRepository masterDataRepository, ITenantDataRepository tenantDataRepository, IPaymentClient paymentClient, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
            this.tenantDataRepository = tenantDataRepository;
            this.paymentClient = paymentClient;
        }

        public async Task<bool> DoInvoicingAsync(Tenant tenant, Used used, CancellationToken stoppingToken)
        {
            var taskDone = true;
            var cardPayment = tenant.Payment?.IsActive == true;

            logger.Event($"Usage {EventNameText(cardPayment)} invoicing tenant '{used.TenantName}' started.");

            (var invoiceTaskDone, var invoice) = await GetInvoiceAsync(tenant, used, cardPayment, stoppingToken);
            if (!invoiceTaskDone)
            {
                taskDone = false;
            }

            if (taskDone)
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (cardPayment)
                {
                    if (used.PaymentStatus == UsagePaymentStatus.None)
                    {
                        if(!await DoPaymentAsync(tenant, used, invoice))
                        {
                            taskDone = false;
                        }
                    }
                    else if (used.PaymentStatus == UsagePaymentStatus.Open || used.PaymentStatus == UsagePaymentStatus.Pending || used.PaymentStatus == UsagePaymentStatus.Authorized)
                    {
                        if(!await UpdatePaymentAsync(used))
                        {
                            taskDone = false;
                        }
                    }

                    if(taskDone)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        if (invoice.SendStatus == UsageInvoiceSendStatus.None && used.PaymentStatus == UsagePaymentStatus.Paid)
                        {
                            if (!await SendInvoiceAsync(used, invoice))
                            {
                                taskDone = false;
                            }
                        }
                    }
                }
                else
                {
                    if (invoice.SendStatus == UsageInvoiceSendStatus.None)
                    {
                        if (!await SendInvoiceAsync(used, invoice))
                        {
                            taskDone = false;
                        }
                    }
                }
            }

            if(taskDone)
            {
                used.IsDone = true;
                await tenantDataRepository.UpdateAsync(used);
                logger.Event($"Usage {EventNameText(cardPayment)} invoicing tenant '{used.TenantName}' done.");
            }
            return taskDone;
        }

        public async Task CreateAndSendCreditNoteAsync(Used used)
        {
            logger.Event($"Usage create and send credit note tenant '{used.TenantName}' started.");

            var invoice = used.Invoices.LastOrDefault();
            if (invoice == null || invoice.IsCreditNote)
            {
                throw new Exception("Invalid last invoice.");
            }
            
            invoice.InvoiceNumber = await GetInvoiceNumberAsync(await GetUsageSettingsAsync()); 

            invoice.IsCreditNote = true;
            used.IsInvoiceReady = false;
            used.Invoices.Add(invoice);
            await tenantDataRepository.UpdateAsync(used);

            logger.Event($"Usage create {EventNameText(invoice.IsCardPayment)} credit note tenant '{used.TenantName}' done.");

            _ = await SendInvoiceAsync(used, invoice);

            logger.Event($"Usage send {EventNameText(invoice.IsCardPayment)} credit note tenant '{used.TenantName}' done.");
        }

        public async Task<bool> SendInvoiceAsync(Used used, Invoice invoice)
        {
            try
            {
                logger.Event($"Usage send {EventNameText(invoice.IsCardPayment)} invoice tenant '{used.TenantName}' started.");
                await CallExternalMakeInvoiceAsync(used, invoice, sendInvoice: true);

                invoice.SendStatus = UsageInvoiceSendStatus.Send;
                await tenantDataRepository.UpdateAsync(used);
                logger.Event($"Usage send {EventNameText(invoice.IsCardPayment)} invoice tenant '{used.TenantName}' done.");
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    invoice.SendStatus = UsageInvoiceSendStatus.Failed;
                    await tenantDataRepository.UpdateAsync(used);
                }
                catch (Exception saveEx)
                {
                    logger.Error(saveEx, $"Unable to save status: {UsageInvoiceSendStatus.Failed}.");
                }
                logger.Error(ex, $"Error occurred during tenant '{used.TenantName}' usage send {EventNameText(invoice.IsCardPayment)} invoice.");
                return false;
            }
        }

        public async Task<bool> DoPaymentAsync(Tenant tenant, Used used, Invoice invoice)
        {
            if (tenant.Payment?.IsActive != true)
            {
                throw new InvalidOperationException("Not an active payment.");
            }

            try
            {
                logger.Event($"Usage payment card invoice tenant '{used.TenantName}' started.");

                var paymentRequest = new PaymentRequest
                {
                    RedirectUrl = "https://www.foxids.com",
                    Amount = new Amount(invoice.Currency, invoice.TotalPrice),
                    Description = "FoxIDs subscription",
                    CustomerId = tenant.Payment.CustomerId,
                    SequenceType = SequenceType.Recurring,
                    MandateId = tenant.Payment.MandateId,
                };

                var paymentResponse = await paymentClient.CreatePaymentAsync(paymentRequest);
                used.PaymentStatus = paymentResponse.Status.FromMollieStatusToPaymentStatus();
                used.PaymentId = paymentResponse.Id;
                await tenantDataRepository.UpdateAsync(used);

                logger.Event($"Usage payment card invoice tenant '{used.TenantName}' status '{used.PaymentStatus}'.");

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    used.PaymentStatus = UsagePaymentStatus.Failed;
                    await tenantDataRepository.UpdateAsync(used);
                }
                catch (Exception saveEx)
                {
                    logger.Error(saveEx, $"Unable to save status: {UsagePaymentStatus.Failed}.");
                }
                logger.Error(ex, $"Error occurred during tenant '{used.TenantName}' usage payment card invoice.");
                return false;
            }
        }

        public async Task<bool> UpdatePaymentAsync(Used used)
        {
            if (used.PaymentId.IsNullOrEmpty())
            {
                throw new InvalidOperationException("The payment id is empty.");
            }

            try
            {
                logger.Event($"Usage read payment card invoice tenant '{used.TenantName}' started.");

                var paymentResponse = await paymentClient.GetPaymentAsync(used.PaymentId);
                used.PaymentStatus = paymentResponse.Status.FromMollieStatusToPaymentStatus();
                await tenantDataRepository.UpdateAsync(used);

                logger.Event($"Usage read payment card invoice tenant '{used.TenantName}' status '{used.PaymentStatus}'.");

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    used.PaymentStatus = UsagePaymentStatus.Failed;
                    await tenantDataRepository.UpdateAsync(used);
                }
                catch (Exception saveEx)
                {
                    logger.Error(saveEx, $"Unable to save status: {UsagePaymentStatus.Failed}.");
                }
                logger.Error(ex, $"Error occurred during tenant '{used.TenantName}' usage read payment card invoice.");
                return false;
            }
        }

        private string EventNameText(bool cardPayment) => cardPayment ? "card" : "payment period";

        private async Task<(bool taskDone, Invoice invoice)> GetInvoiceAsync(Tenant tenant, Used used, bool cardPayment, CancellationToken stoppingToken)
        {
            try
            {
                if (used.IsInvoiceReady)
                {
                    return (true, used.Invoices.Last());
                }
                else
                {
                    logger.Event($"Usage create {EventNameText(cardPayment)} invoice tenant '{used.TenantName}' started.");
                    var invoice = await CreateInvoiceAsync(tenant, used, cardPayment, stoppingToken);
                    logger.Event($"Usage create {EventNameText(cardPayment)} invoice tenant '{used.TenantName}' done.");
                    return (true, invoice);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error occurred during tenant '{tenant.Name}' usage {EventNameText(cardPayment)} invoicing.");
                return (false, null);
            }
        }

        private async Task<Invoice> CreateInvoiceAsync(Tenant tenant, Used used, bool isCardPayment, CancellationToken stoppingToken)
        {
            var plan = await masterDataRepository.GetAsync<Plan>(await Plan.IdFormatAsync(tenant.PlanName));

            if (plan.CostPerMonth <= 0)
            {
                throw new Exception($"Tenant '{RouteBinding.TenantName}' plan '{tenant.PlanName}' do not have a cost per month.");
            }

            stoppingToken.ThrowIfCancellationRequested();

            var invoice = new Invoice
            {
                IsCardPayment = isCardPayment,
                IssueDate = DateTime.Now,
                Seller = mapper.Map<Seller>(settings.Usage.Seller),
                Customer = tenant.Customer,
                Currency = tenant.Currency.IsNullOrEmpty() ? Constants.Models.Currency.Eur : tenant.Currency,
                BankDetails = settings.Usage.Seller.BankDetails,
            };

            var usageSettings = await GetUsageSettingsAsync();
            CalculateInvoice(invoice, used, plan, tenant.IncludeVat, GetExchangesRate(invoice.Currency, usageSettings.CurrencyExchanges));
            if(invoice.Price <= 0)
            {
                return null;
            }

            invoice.InvoiceNumber = await GetInvoiceNumberAsync(usageSettings);

            if (!isCardPayment)
            {
                invoice.DueDate = invoice.IssueDate + TimeSpan.FromDays(settings.Usage.Seller.PaymentDueDays);
            }

            await invoice.ValidateObjectAsync();
            used.IsInvoiceReady = true;
            if (used.Invoices?.Count > 0)
            {
                used.Invoices.Add(invoice);
            }
            else
            {
                used.Invoices = [invoice];
            }
            stoppingToken.ThrowIfCancellationRequested(); 
            await tenantDataRepository.UpdateAsync(used);
            return invoice;
        }

        private async Task<UsageSettings> GetUsageSettingsAsync()
        {
            var usageSettings = await masterDataRepository.GetAsync<UsageSettings>(await UsageSettings.IdFormatAsync(), required: false);
            if (usageSettings == null)
            {
                usageSettings = new UsageSettings
                {
                    Id = await UsageSettings.IdFormatAsync()
                };
                await masterDataRepository.CreateAsync(usageSettings);
            }

            return usageSettings;
        }

        private async Task<string> GetInvoiceNumberAsync(UsageSettings usageSettings)
        {
            usageSettings.InvoiceNumber += 1;
            await masterDataRepository.UpdateAsync(usageSettings);

            var fullInvoiceNumber = $"{(usageSettings.InvoiceNumberPrefix.IsNullOrWhiteSpace() ? string.Empty : $"{usageSettings.InvoiceNumberPrefix}-")}{usageSettings.InvoiceNumber}";
            return fullInvoiceNumber;
        }

        private decimal GetExchangesRate(string currency, List<UsageCurrencyExchange> currencyExchanges)
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
            var invoiceDays = (used.PeriodEndDate - used.PeriodBeginDate).Days;
            if (invoiceDays > 10)
            {
                if(used.PeriodBeginDate.Day == 1)
                {
                    invoice.Price = RoundPrice(plan.CostPerMonth);
                }
                else
                {
                    invoice.Price = RoundPrice(plan.CostPerMonth / DateTime.DaysInMonth(used.PeriodYear, used.PeriodMonth) * invoiceDays);
                }
            }

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

        private async Task<ExtInv.InvoiceResponse> CallExternalMakeInvoiceAsync(Used used, Invoice invoice, bool sendInvoice)
        {
            var invoiceRequest = mapper.Map<ExtInv.InvoiceRequest>(invoice);
            invoiceRequest.SendInvoice = sendInvoice;
            invoiceRequest.PeriodBeginDate = used.PeriodBeginDate;
            invoiceRequest.PeriodEndDate = used.PeriodEndDate;
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
