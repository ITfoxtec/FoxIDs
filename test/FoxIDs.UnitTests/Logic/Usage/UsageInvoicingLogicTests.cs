using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Logic.Usage;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using Moq;
using Mollie.Api.Client.Abstract;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ExtInv = FoxIDs.Models.ExternalInvoices;

namespace FoxIDs.UnitTests.Logic.Usage
{
    public class UsageInvoicingLogicTests
    {
        [Fact]
        public async Task DoInvoicingAsync_WithPaidCardAndSentInvoice_DoesNotResendAndReportsNotDone()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant1", TrackName = "master" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var settings = new FoxIDsControlSettings
            {
                FoxIDsEndpoint = "https://foxids.test",
                FoxIDsControlEndpoint = "https://control.foxids.test",
                Payment = new PaymentSettings(),
                Usage = new UsageBaseSettings
                {
                    Seller = new UsageSellerSettings { FromEmail = "from@example.com" },
                    ExternalInvoiceApiSecret = "secret",
                    ExternalInvoiceApiUrl = "https://invoice.test"
                }
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var mapperMock = new Mock<IMapper>(MockBehavior.Strict);
            var masterRepositoryMock = new Mock<IMasterDataRepository>(MockBehavior.Strict);
            var tenantRepositoryMock = new Mock<ITenantDataRepository>(MockBehavior.Strict);

            tenantRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Used>(), It.IsAny<TelemetryScopedLogger>())).Returns(ValueTask.CompletedTask);

            var usageMolliePaymentLogic = new UsageMolliePaymentLogic(logger, tenantRepositoryMock.Object, Mock.Of<IMandateClient>(), Mock.Of<IPaymentClient>(), null);
            var logic = new UsageInvoicingLogic(settings, logger, httpClientFactoryMock.Object, mapperMock.Object, masterRepositoryMock.Object, tenantRepositoryMock.Object, usageMolliePaymentLogic, httpContextAccessor);

            var tenant = new Tenant
            {
                Name = "tenant1",
                EnableUsage = true,
                DoPayment = true,
                Payment = new Payment { IsActive = true, MandateId = "mandate-1", CustomerId = "customer-1" }
            };

            var invoice = new Invoice
            {
                InvoiceNumber = "INV-1",
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow).ToDateOnlySerializable(),
                Currency = Constants.Models.Currency.Eur,
                Seller = new Seller { FromEmail = "from@example.com" },
                Customer = new Customer(),
                SendStatus = UsageInvoiceSendStatus.Send
            };

            var used = new Used
            {
                Id = await Used.IdFormatAsync(tenant.Name, 2024, 1),
                TenantName = tenant.Name,
                PeriodBeginDate = new DateOnly(2024, 1, 1).ToDateOnlySerializable(),
                PeriodEndDate = new DateOnly(2024, 1, 31).ToDateOnlySerializable(),
                PaymentStatus = UsagePaymentStatus.Paid,
                IsInvoiceReady = true,
                Invoices = new List<Invoice> { invoice }
            };

            var result = await logic.DoInvoicingAsync(tenant, used, CancellationToken.None);

            Assert.False(result);
            Assert.False(used.IsDone);
            tenantRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Used>(), It.IsAny<TelemetryScopedLogger>()), Times.Never);
            httpClientFactoryMock.Verify(c => c.CreateClient(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DoInvoicingAsync_WithPaidCardAndFailedSendStatus_DoesNotRetryAndReportsNotDone()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant1", TrackName = "master" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var settings = new FoxIDsControlSettings
            {
                FoxIDsEndpoint = "https://foxids.test",
                FoxIDsControlEndpoint = "https://control.foxids.test",
                Payment = new PaymentSettings(),
                Usage = new UsageBaseSettings
                {
                    Seller = new UsageSellerSettings { FromEmail = "from@example.com" },
                    ExternalInvoiceApiSecret = "secret",
                    ExternalInvoiceApiUrl = "https://invoice.test/api",
                    ExternalInvoiceApiId = "invoice"
                }
            };

            Used used = null;
            var mapperMock = new Mock<IMapper>(MockBehavior.Strict);
            mapperMock.Setup(m => m.Map<ExtInv.InvoiceRequest>(It.IsAny<Invoice>())).Returns<Invoice>(inv => new ExtInv.InvoiceRequest
            {
                InvoiceNumber = inv.InvoiceNumber,
                IssueDate = inv.IssueDate.ToDateOnly(),
                DueDate = inv.DueDate?.ToDateOnly(),
                TenantName = used?.TenantName,
                PeriodBeginDate = used?.PeriodBeginDate.ToDateOnly() ?? default,
                PeriodEndDate = used?.PeriodEndDate.ToDateOnly() ?? default,
                Currency = inv.Currency,
                IsCardPayment = inv.IsCardPayment,
                IsPaid = true,
                Lines = [new ExtInv.InvoiceLine { Text = inv.Lines[0].Text, Quantity = inv.Lines[0].Quantity, UnitPrice = inv.Lines[0].UnitPrice, Price = inv.Lines[0].Price }],
                Price = inv.Price,
                Vat = inv.Vat,
                TotalPrice = inv.TotalPrice,
                Seller = new ExtInv.Seller { Name = inv.Seller.Name, FromEmail = inv.Seller.FromEmail, BccEmails = inv.Seller.BccEmails },
                Customer = new ExtInv.Customer { Name = inv.Customer.Name, InvoiceEmails = inv.Customer.InvoiceEmails },
                BankDetails = inv.BankDetails,
                IncludesUsage = inv.IncludesUsage,
                TimeItems = null
            });

            var httpHandler = new StubHttpMessageHandler();
            var httpClient = new HttpClient(httpHandler);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var masterRepositoryMock = new Mock<IMasterDataRepository>(MockBehavior.Strict);
            var tenantRepositoryMock = new Mock<ITenantDataRepository>(MockBehavior.Strict);
            tenantRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Used>(), It.IsAny<TelemetryScopedLogger>())).Returns(ValueTask.CompletedTask);

            var usageMolliePaymentLogic = new UsageMolliePaymentLogic(logger, tenantRepositoryMock.Object, Mock.Of<IMandateClient>(), Mock.Of<IPaymentClient>(), null);
            var logic = new UsageInvoicingLogic(settings, logger, httpClientFactoryMock.Object, mapperMock.Object, masterRepositoryMock.Object, tenantRepositoryMock.Object, usageMolliePaymentLogic, httpContextAccessor);

            var tenant = new Tenant
            {
                Name = "tenant1",
                EnableUsage = true,
                DoPayment = true,
                Payment = new Payment { IsActive = true, MandateId = "mandate-1", CustomerId = "customer-1" }
            };

            var invoice = new Invoice
            {
                InvoiceNumber = "INV-2",
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow).ToDateOnlySerializable(),
                Currency = Constants.Models.Currency.Eur,
                Seller = new Seller { Name = "FoxIDs", FromEmail = "from@example.com", BccEmails = new List<string> { "billing@example.com" } },
                Customer = new Customer { Name = "Customer", InvoiceEmails = new List<string> { "customer@example.com" } },
                Lines = new List<InvoiceLine> { new InvoiceLine { Text = "line", Quantity = 1, UnitPrice = 10, Price = 10 } },
                Price = 10,
                Vat = 0,
                TotalPrice = 10,
                SendStatus = UsageInvoiceSendStatus.Failed
            };

            used = new Used
            {
                Id = await Used.IdFormatAsync(tenant.Name, 2024, 1),
                TenantName = tenant.Name,
                PeriodBeginDate = new DateOnly(2024, 1, 1).ToDateOnlySerializable(),
                PeriodEndDate = new DateOnly(2024, 1, 31).ToDateOnlySerializable(),
                PaymentStatus = UsagePaymentStatus.Paid,
                IsInvoiceReady = true,
                Invoices = new List<Invoice> { invoice }
            };

            var invoiceRequest = mapperMock.Object.Map<ExtInv.InvoiceRequest>(invoice);
            await invoiceRequest.ValidateObjectAsync();

            var result = await logic.DoInvoicingAsync(tenant, used, CancellationToken.None);

            Assert.False(httpHandler.RequestSent);
            Assert.Equal(UsageInvoiceSendStatus.Failed, used.Invoices[0].SendStatus);
            Assert.False(result);
            Assert.False(used.IsDone);
            httpClientFactoryMock.Verify(c => c.CreateClient(It.IsAny<string>()), Times.Never);
            tenantRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Used>(), It.IsAny<TelemetryScopedLogger>()), Times.Never);
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            public bool RequestSent { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestSent = true;
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"invoiceNumber\":\"INV-2\"}")
                };

                return Task.FromResult(response);
            }
        }
    }
}
