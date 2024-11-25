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
using System.Threading;
using System.Threading.Tasks;
using ExtInv = FoxIDs.Models.ExternalInvoices;

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
        private readonly UsageMolliePaymentLogic usageMolliePaymentLogic;

        public UsageInvoicingLogic(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, IMapper mapper, IMasterDataRepository masterDataRepository, ITenantDataRepository tenantDataRepository, UsageMolliePaymentLogic usageMolliePaymentLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
            this.tenantDataRepository = tenantDataRepository;
            this.usageMolliePaymentLogic = usageMolliePaymentLogic;
        }

        public async Task<bool> DoInvoicingAsync(Tenant tenant, Used used, CancellationToken stoppingToken, bool doInvoicing = false)
        {
            if(!doInvoicing && tenant.EnableUsage != true)
            {
                used.IsInactive = true;
                await tenantDataRepository.UpdateAsync(used);
                logger.Event($"Usage, invoicing for tenant '{used.TenantName}' not enabled, set to inactive.");
                return true;
            }

            var taskDone = true;
            var isCardPayment = tenant.DoPayment == true || usageMolliePaymentLogic.HasActiveCardPayment(tenant);

            logger.Event($"Usage, {EventNameText(isCardPayment)} invoicing for tenant '{used.TenantName}' started.");

            (var invoiceTaskDone, var invoice) = await GetInvoiceAsync(tenant, used, isCardPayment, stoppingToken);
            if (!invoiceTaskDone)
            {
                taskDone = false;
            }

            if (taskDone)
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (isCardPayment)
                {
                    if (!usageMolliePaymentLogic.HasActiveCardPayment(tenant))
                    {
                        used.PaymentStatus = UsagePaymentStatus.Failed;
                        await tenantDataRepository.UpdateAsync(used);
                        try
                        {
                            throw new Exception("Card payment NOT active.");
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                            taskDone = false;
                        }
                    }
                    else
                    {
                        if (used.PaymentStatus == UsagePaymentStatus.None)
                        {
                            if (!await usageMolliePaymentLogic.DoPaymentAsync(tenant, used, invoice))
                            {
                                taskDone = false;
                            }
                        }
                        else if (used.PaymentStatus == UsagePaymentStatus.Open || used.PaymentStatus == UsagePaymentStatus.Pending || used.PaymentStatus == UsagePaymentStatus.Authorized)
                        {
                            if (!await usageMolliePaymentLogic.UpdatePaymentAsync(used))
                            {
                                taskDone = false;
                            }
                        }

                        if (taskDone)
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
                used.IsInactive = false;
                used.IsDone = true;
                await tenantDataRepository.UpdateAsync(used);
                logger.Event($"Usage, {EventNameText(isCardPayment)} invoicing for tenant '{used.TenantName}' done.");
            }
            return taskDone;
        }

        public async Task CreateAndSendCreditNoteAsync(Used used)
        {
            if (used.PaymentStatus != UsagePaymentStatus.None && !used.PaymentStatus.PaymentStatusIsGenerallyFailed())
            {
                throw new Exception("Invalid payment status.");
            }

            logger.Event($"Usage, create and send credit note for tenant '{used.TenantName}' started.");

            var invoice = used.Invoices.LastOrDefault();
            if (invoice == null || invoice.IsCreditNote)
            {
                throw new Exception("Invalid last invoice.");
            }

            var creditNote = new Invoice
            {
                IsCreditNote = true,
                InvoiceNumber = await GetInvoiceNumberAsync(await GetUsageSettingsAsync()),
                IsCardPayment = invoice.IsCardPayment,
                IssueDate = DateOnly.FromDateTime(DateTime.Now),
                Seller = mapper.Map<Seller>(settings.Usage.Seller),
                Customer = invoice.Customer,
                Currency = invoice.Currency,
                Lines = invoice.Lines,
                Price = invoice.Price,
                Vat = invoice.Vat,
                TotalPrice = invoice.TotalPrice,
            };

            used.IsInvoiceReady = false;
            used.IsDone = false;
            used.Invoices.Add(creditNote);
            await tenantDataRepository.UpdateAsync(used);

            logger.Event($"Usage, create {EventNameText(creditNote.IsCardPayment)} credit note for tenant '{used.TenantName}' done.");

            _ = await SendInvoiceAsync(used, creditNote);

            logger.Event($"Usage, send {EventNameText(creditNote.IsCardPayment)} credit note for tenant '{used.TenantName}' done.");
        }

        public async Task<bool> SendInvoiceAsync(Used used, Invoice invoice)
        {
            try
            {
                logger.Event($"Usage, send {EventNameText(invoice.IsCardPayment)} invoice for tenant '{used.TenantName}' started.");
                await CallExternalMakeInvoiceAsync(used, invoice, sendInvoice: true);

                invoice.SendStatus = UsageInvoiceSendStatus.Send;
                await tenantDataRepository.UpdateAsync(used);
                logger.Event($"Usage, send {EventNameText(invoice.IsCardPayment)} invoice for tenant '{used.TenantName}' done.");
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    invoice.SendStatus = UsageInvoiceSendStatus.Failed;
                    await tenantDataRepository.UpdateAsync(used);
                }
                            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
                catch (Exception saveEx)
                {
                    logger.Error(saveEx, $"Usage, unable to save status: {UsageInvoiceSendStatus.Failed}.");
                }
                logger.Error(ex, $"Usage, send {EventNameText(invoice.IsCardPayment)} invoice for tenant '{used.TenantName}' error.");
                return false;
            }
        }

        private async Task<(bool taskDone, Invoice invoice)> GetInvoiceAsync(Tenant tenant, Used used, bool isCardPayment, CancellationToken stoppingToken)
        {
            try
            {
                (bool taskDone, Invoice invoice) = await GetInvoiceInternalAsync(tenant, used, stoppingToken);
                invoice.IsCardPayment = isCardPayment;
                if (isCardPayment)
                {
                    invoice.DueDate = null;
                }
                else
                {
                    invoice.DueDate = invoice.IssueDate.AddDays(settings.Usage.Seller.PaymentDueDays);
                }
                logger.Event($"Usage, get {EventNameText(isCardPayment)} invoice for tenant '{used.TenantName}'.");
                return (taskDone, invoice);
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
                logger.Error(ex, $"Usage, create {EventNameText(isCardPayment)} invoice for tenant '{used.TenantName}' error.");
                return (false, null);
            }
        }

        private async Task<(bool taskDone, Invoice invoice)> GetInvoiceInternalAsync(Tenant tenant, Used used, CancellationToken stoppingToken)
        {
            if (used.IsInvoiceReady)
            {
                return (true, used.Invoices.Last());
            }
            else
            {
                logger.Event($"Usage, create invoice for tenant '{used.TenantName}'.");
                var invoice = await CreateInvoiceAsync(tenant, used, stoppingToken);
                return (true, invoice);
            }
        }

        private async Task<Invoice> CreateInvoiceAsync(Tenant tenant, Used used, CancellationToken stoppingToken)
        {
            var invoice = new Invoice
            {
                IssueDate = DateOnly.FromDateTime(DateTime.Now),
                Seller = mapper.Map<Seller>(settings.Usage.Seller),
                Customer = tenant.Customer,
                Currency = tenant.Currency.IsNullOrEmpty() ? Constants.Models.Currency.Eur : tenant.Currency,
                BankDetails = settings.Usage.Seller.BankDetails,
            };

            var usageSettings = await GetUsageSettingsAsync();
            var plan = tenant.EnableUsage == true && !tenant.PlanName.IsNullOrEmpty() ? await masterDataRepository.GetAsync<Plan>(await Plan.IdFormatAsync(tenant.PlanName)) : null;

            stoppingToken.ThrowIfCancellationRequested();
            CalculateInvoice(used, plan, invoice, tenant.IncludeVat == true, GetExchangesRate(invoice.Currency, usageSettings.CurrencyExchanges));

            invoice.InvoiceNumber = await GetInvoiceNumberAsync(usageSettings);

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

            var fullInvoiceNumber = $"{(usageSettings.InvoiceNumberPrefix.IsNullOrWhiteSpace() ? string.Empty : usageSettings.InvoiceNumberPrefix)}{usageSettings.InvoiceNumber}";
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

        private void CalculateInvoice(Used used, Plan plan, Invoice invoice, bool includeVat, decimal exchangeRate)
        {
            invoice.Lines = new List<InvoiceLine>();

            if (plan != null)
            {
                invoice.IncludesUsage = true;

                (var planPrice, var planInfoText) = MonthUsagePrice(used, plan, exchangeRate);
                invoice.Lines.Add(new InvoiceLine { Text = $"FoxIDs {plan.DisplayName ?? plan.Name} plan{planInfoText}", Quantity = 1, UnitPrice = planPrice, Price = planPrice });
                invoice.Price += planPrice;

                invoice.Price += AddInvoiceUsageLine(invoice.Lines, $"Additional environments ({plan.Tracks.Included} included)", $"Additional environments (more then {plan.Tracks.FirstLevelThreshold})", used.Tracks, plan.Tracks, exchangeRate);
                invoice.Price += AddInvoiceUsageLine(invoice.Lines, $"Additional users ({plan.Users.Included} included)", $"Additional users (more then {plan.Users.FirstLevelThreshold})", used.Users, plan.Users, exchangeRate);
                invoice.Price += AddInvoiceUsageLine(invoice.Lines, $"Additional logins ({plan.Logins.Included} included)", $"Additional logins (more then {plan.Logins.FirstLevelThreshold})", used.Logins, plan.Logins, exchangeRate);
                invoice.Price += AddInvoiceUsageLine(invoice.Lines, $"Additional token requests ({plan.TokenRequests.Included} included)", $"Additional token requests (more then {plan.TokenRequests.FirstLevelThreshold})", used.TokenRequests, plan.TokenRequests, exchangeRate);
                invoice.Price += AddInvoiceUsageLine(invoice.Lines, $"Additional Control API reads ({plan.ControlApiGetRequests.Included} included)", $"Additional Control API reads (more then {plan.ControlApiGetRequests.FirstLevelThreshold})", used.ControlApiGets, plan.ControlApiGetRequests, exchangeRate);
                invoice.Price += AddInvoiceUsageLine(invoice.Lines, $"Additional Control API updates ({plan.ControlApiUpdateRequests.Included} included)", $"Additional Control API updates (more then {plan.ControlApiUpdateRequests.FirstLevelThreshold})", used.ControlApiUpdates, plan.ControlApiUpdateRequests, exchangeRate);
            }

            if (used.Items?.Count() > 0)
            {
                foreach (var item in used.Items.Where(i => i.Type == UsedItemTypes.Text))
                {
                    var price = RoundPrice(item.UnitPrice * item.Quantity, false);
                    invoice.Lines.Add(new InvoiceLine { Text = item.Text, Quantity = item.Quantity, UnitPrice = item.UnitPrice, Price = price });
                    invoice.Price += price;
                }

                if (used.Items.Where(i => i.Type == UsedItemTypes.Hours).Count() > 0)
                {
                    invoice.TimeItems = new List<UsedItem>();
                    decimal totalTimePrice = 0;
                    decimal totalTimeHours = 0;
                    foreach (var item in used.Items.Where(i => i.Type == UsedItemTypes.Hours))
                    {
                        invoice.TimeItems.Add(item);
                        totalTimeHours += item.Quantity;
                        totalTimePrice += RoundPrice(item.UnitPrice * item.Quantity, true);
                    }
                    var price = RoundPrice(totalTimePrice, false);
                    var unitPrice = RoundPrice(totalTimePrice / totalTimeHours, true);
                    invoice.Lines.Add(new InvoiceLine { Text = "Time added together", Quantity = totalTimeHours, UnitPrice = unitPrice, Price = price });
                    invoice.Price += price;
                }
            }

            if (includeVat)
            {
                invoice.Vat = RoundPrice(invoice.Price * settings.Usage.VatPercent / 100, false);
            }
            invoice.TotalPrice = invoice.Price + invoice.Vat;
        }

        private (decimal planPrice, string planInfoText) MonthUsagePrice(Used used, Plan plan, decimal exchangeRate)
        {
            var invoiceDays = used.PeriodEndDate.Day - used.PeriodBeginDate.Day;
            if (invoiceDays > 10)
            {
                if (used.PeriodBeginDate.Day == 1)
                {
                    return (CurrencyAndRoundPrice(plan.CostPerMonth, exchangeRate, false), string.Empty);
                }
                else
                {
                    var price = CurrencyAndRoundPrice(plan.CostPerMonth / DateTime.DaysInMonth(used.PeriodBeginDate.Year, used.PeriodBeginDate.Month) * invoiceDays, exchangeRate, false);
                    return (price, " (first month reduced price)");
                }
            }
            else
            {
                return (0, " (first short month free)");
            }
        }

        private decimal AddInvoiceUsageLine(List<InvoiceLine> lines, string textFirstLevel, string textSecondLevel, decimal usedCount, PlanItem planItem, decimal exchangeRate)
        {
            decimal price = 0;

            var firstLevel = planItem.FirstLevelThreshold > 0 && usedCount > planItem.FirstLevelThreshold ? Convert.ToDecimal(planItem.FirstLevelThreshold) : usedCount;
            var firstLevelUnitPrice = CurrencyAndRoundPrice(planItem.FirstLevelCost, exchangeRate, true);
            var firstLevelQuantity = usedCount > planItem.Included ? firstLevel - planItem.Included : 0;
            var firstLevelPrice = RoundPrice(firstLevelUnitPrice * firstLevelQuantity, false);
            lines.Add(new InvoiceLine { Text = textFirstLevel, Quantity = firstLevelQuantity, UnitPrice = firstLevelUnitPrice, Price = firstLevelPrice });
            price += firstLevelPrice;

            if (planItem.FirstLevelThreshold > 0 && usedCount > planItem.FirstLevelThreshold)
            {
                var secondLevelUnitPrice = CurrencyAndRoundPrice(planItem.SecondLevelCost.Value, exchangeRate, true);
                var secundLevelPriceQuantity = usedCount - planItem.FirstLevelThreshold.Value;
                var secundLevelPrice = RoundPrice(secondLevelUnitPrice * secundLevelPriceQuantity, false);
                lines.Add(new InvoiceLine { Text = textSecondLevel, Quantity = secundLevelPriceQuantity, UnitPrice = secondLevelUnitPrice, Price = secundLevelPrice });
                price += secundLevelPrice;
            }

            return price;
        }

        private decimal CurrencyAndRoundPrice(decimal price, decimal exchangeRate, bool isUnitPrice)
        {
            return RoundPrice(price * exchangeRate, isUnitPrice);
        }

        private decimal RoundPrice(decimal price, bool isUnitPrice)
        {
            return decimal.Round(price, isUnitPrice ? 6 : 2);
        }

        private async Task<ExtInv.InvoiceResponse> CallExternalMakeInvoiceAsync(Used used, Invoice invoice, bool sendInvoice)
        {
            var invoiceRequest = mapper.Map<ExtInv.InvoiceRequest>(invoice);
            invoiceRequest.SendInvoice = sendInvoice;
            invoiceRequest.IsPaid = used.PaymentStatus == UsagePaymentStatus.Paid;
            invoiceRequest.TenantName = used.TenantName;
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

        private string EventNameText(bool isCardPayment) => isCardPayment ? "'card'" : "'payment period'";
    }
}
