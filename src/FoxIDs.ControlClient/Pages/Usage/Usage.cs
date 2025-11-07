using Blazored.Toast.Services;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages.Usage
{
    public partial class Usage
    {
        private string usageTenantsHref;
        private string usageSettingsHref;
        private FoxIDs.Models.Api.UsageSettings usageSettings;
        private PageEditForm<FilterUsageViewModel> searchUsageForm;
        private List<GeneralUsedViewModel> usedList = new List<GeneralUsedViewModel>();
        private string paginationToken;

        [Inject]
        public ClientSettings ClientSettings { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public HelpersService HelpersService { get; set; }

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public TenantService TenantService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            usageTenantsHref = $"{TenantName}/usagetenants";
            usageSettingsHref = $"{TenantName}/usagesettings";
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            searchUsageForm?.ClearError();
            try
            {
                var thisMonth = DateTimeOffset.Now;
                SetGeneralUsageList(await TenantService.GetUsagesAsync(null, thisMonth.Year, thisMonth.Month, cancellationToken: PageCancellationToken));
                usageSettings = await TenantService.GetUsageSettingsAsync(cancellationToken: PageCancellationToken);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                usedList?.Clear();
                searchUsageForm.SetError(ex.Message);
            }
        }

        private void SetGeneralUsageList(PaginationResponse<UsedBase> filterUsedList, bool addTenants = false)
        {
            var gul = new List<GeneralUsedViewModel>();
            foreach (var u in filterUsedList.Data)
            {
                gul.Add(new GeneralUsedViewModel(u));
            }
            if (usedList != null && addTenants)
            {
                usedList.AddRange(gul);
            }
            else
            {
                usedList = gul;
            }
            paginationToken = filterUsedList.PaginationToken;
        }

        private void ShowCreateUsage()
        {
            var used = new GeneralUsedViewModel
            {
                CreateMode = true,
                Edit = true
            };
            usedList.Add(used);
        }

        public bool ShowDoInvoicingButton(GeneralUsedViewModel used) => (used.IsUsageCalculated || used.HasItems) && !used.IsDone;

        public bool ShowSendInvoiceAgainButton(GeneralUsedViewModel used) => used.IsInvoiceReady && (used.Invoices?.LastOrDefault()?.IsCardPayment != true || used.PaymentStatus == UsagePaymentStatus.Paid);

        public bool ShowDoCreditNoteButton(GeneralUsedViewModel used) => used.Invoices?.LastOrDefault().SendStatus == UsageInvoiceSendStatus.Send && (used.PaymentStatus == UsagePaymentStatus.None || used.PaymentStatus.PaymentApiStatusIsGenerallyFailed());

        public bool ShowSendCreditNoteAgainButton(GeneralUsedViewModel used) => !used.IsInvoiceReady && used.Invoices?.LastOrDefault()?.IsCreditNote == true;

        public bool ShowDoPaymentButton(GeneralUsedViewModel used) => used.IsInvoiceReady && used.PaymentStatus.PaymentApiStatusIsGenerallyFailed();

        public bool ShowMarkAsPaidButton(GeneralUsedViewModel used) => used.IsInvoiceReady && ((used.Invoices?.LastOrDefault()?.IsCardPayment != true && used.PaymentStatus != UsagePaymentStatus.Paid) || used.PaymentStatus.PaymentApiStatusIsGenerallyFailed());

        public bool ShowMarkAsNotPaidButton(GeneralUsedViewModel used) => used.IsInvoiceReady && used.PaymentStatus == UsagePaymentStatus.Paid;

        private async Task ShowUpdateUsageAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.CreateMode = false;
            generalUsed.ShowAdvanced = false;
            generalUsed.Error = null;
            generalUsed.Edit = true;

            try
            {
                var used = await TenantService.GetUsageAsync(new UsageRequest { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate }, cancellationToken: PageCancellationToken);
                await generalUsed.Form.InitAsync(used.Map<UsedViewModel>());
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalUsed.Error = ex.Message;
            }
        }
        private string UsageInfoAndPriceText(GeneralUsedViewModel generalUsed)
        {
            var invoice = generalUsed.Invoices?.LastOrDefault();
            var price = invoice?.Price;
            var totalPrice = invoice?.TotalPrice;
            return $"{(invoice != null ? $", Invoice: {invoice.InvoiceNumber}" : string.Empty)}{(totalPrice > 0 ? $",{(totalPrice != invoice.Price ? $" Price: {invoice?.Currency}{price}," : string.Empty)} Total price: {invoice?.Currency}{totalPrice}" : string.Empty)}";
        }

        private (bool sendItemsInvoice, bool failed, bool notPaid, bool paid, string statusText) UsageInfoText(GeneralUsedViewModel generalUsed)
        {
            var invoice = generalUsed.Invoices?.LastOrDefault();
            var invoiceSendState = invoice?.SendStatus;

            var statusTest = new List<string>();
            if (generalUsed.IsUsageCalculated)
            {
                statusTest.Add("usage calculated");
            }

            if (invoiceSendState.HasValue)
            {
                statusTest.Add($"invoice {invoiceSendState}".ToLower());
            }
            else if (generalUsed.IsInvoiceReady)
            {
                statusTest.Add("invoice created");
            }

            if (generalUsed.PaymentStatus != UsagePaymentStatus.None)
            {
                if (generalUsed.PaymentStatus == UsagePaymentStatus.Paid)
                {
                    statusTest.Add(generalUsed.PaymentStatus.ToString().ToLower());
                }
                else
                {
                    statusTest.Add($"payment {generalUsed.PaymentStatus}".ToLower());
                }
            }

            if (generalUsed.IsDone)
            {
                statusTest.Add("task is done");
            }
            else
            {
                if (generalUsed.IsInactive)
                {
                    statusTest.Add("task is inactive");
                }
            }

            if(statusTest.Count() <= 0)
            {
                statusTest.Add("registered");
            }

            var sendItemsInvoice = generalUsed.HasItems && invoice?.SendStatus != UsageInvoiceSendStatus.Send;
            var failed = invoice?.SendStatus == UsageInvoiceSendStatus.Failed || generalUsed?.PaymentStatus.PaymentApiStatusIsGenerallyFailed() == true;
            var notPaid = generalUsed.IsInvoiceReady && ((invoice?.IsCardPayment != true && generalUsed.PaymentStatus != UsagePaymentStatus.Paid) || generalUsed.PaymentStatus.PaymentApiStatusIsGenerallyFailed());
            return (sendItemsInvoice, failed, notPaid, generalUsed.PaymentStatus == UsagePaymentStatus.Paid, $"Status: {string.Join(", ", statusTest)}");
        }

        private void OnUsageFilterAfterInit(FilterUsageViewModel filterUsage)
        {
            var thisMonth = DateTimeOffset.Now;
            filterUsage.PeriodYear = thisMonth.Year;
            filterUsage.PeriodMonth = thisMonth.Month;
        }

        private async Task OnUsageStepAsync(bool moveRight)
        {
            try
            {
                var date = new DateOnly(searchUsageForm.Model.PeriodYear, searchUsageForm.Model.PeriodMonth, 1);
                if (!moveRight)
                {
                    date = date.AddMonths(-1);
                }
                else
                {
                    date = date.AddMonths(1);
                }
                searchUsageForm.Model.PeriodYear = date.Year;
                searchUsageForm.Model.PeriodMonth = date.Month;
                SetGeneralUsageList(await TenantService.GetUsagesAsync(searchUsageForm.Model.FilterTenantValue, searchUsageForm.Model.PeriodYear, searchUsageForm.Model.PeriodMonth, cancellationToken: PageCancellationToken));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                searchUsageForm.SetError(ex.Message);
            }
        }

        private async Task OnUsageFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralUsageList(await TenantService.GetUsagesAsync(searchUsageForm.Model.FilterTenantValue, searchUsageForm.Model.PeriodYear, searchUsageForm.Model.PeriodMonth, cancellationToken: PageCancellationToken));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    searchUsageForm.SetFieldError(nameof(searchUsageForm.Model.FilterTenantValue), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task LoadMoreUsagesAsync()
        {
            try
            {
                SetGeneralUsageList(await TenantService.GetUsagesAsync(searchUsageForm.Model.FilterTenantValue, searchUsageForm.Model.PeriodYear, searchUsageForm.Model.PeriodMonth, paginationToken: paginationToken, cancellationToken: PageCancellationToken), addTenants: true);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    searchUsageForm.SetFieldError(nameof(searchUsageForm.Model.FilterTenantValue), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task UsedViewModelAfterInitAsync(GeneralUsedViewModel generalUsed, UsedViewModel used)
        {
            if (generalUsed.CreateMode)
            {
                generalUsed.PeriodBeginDate = used.PeriodBeginDate = new DateOnly(searchUsageForm.Model.PeriodYear, searchUsageForm.Model.PeriodMonth, 1);
                generalUsed.PeriodEndDate = used.PeriodEndDate = used.PeriodBeginDate.AddMonths(1).AddDays(-1);
            }
            else if (generalUsed.Edit)
            {
                await SetHourPrice(generalUsed);
            }
        }

        private async Task SetHourPrice(GeneralUsedViewModel generalUsed)
        {
            try
            {
                var tenant = await TenantService.GetTenantAsync(generalUsed.TenantName, cancellationToken: PageCancellationToken);
                generalUsed.EnableUsage = tenant.EnableUsage;
                if (tenant.HourPrice > 0)
                {
                    generalUsed.HourPrice = tenant.HourPrice.Value;
                }
                else
                {
                    var rate = GetExchangesRate(tenant.Currency, usageSettings.CurrencyExchanges);
                    generalUsed.HourPrice = decimal.Round(usageSettings.HourPrice * rate, 2);
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
        }

        private decimal GetExchangesRate(string currency, List<UsageCurrencyExchange> currencyExchanges)
        {
            if (currency.IsNullOrEmpty() || currency == Constants.Models.Currency.Eur)
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

        private void UsedCancel(GeneralUsedViewModel used)
        {
            if (used.CreateMode)
            {
                usedList.Remove(used);
            }
            else
            {
                used.Edit = false;
            }
        }

        private void AddItem(MouseEventArgs e, GeneralUsedViewModel generalUsed, List<UsedItem> items, UsedItemTypes type)
        {
            var item = new UsedItem { Type = type };
            if (type == UsedItemTypes.Hours)
            {
                item.UnitPrice = generalUsed.HourPrice;
            }
            items.Add(item);
        }

        private void RemoveItem(MouseEventArgs e, List<UsedItem> items, UsedItem item)
        {
            items.Remove(item);
        }

        private async Task OnEditUsedValidSubmitAsync(GeneralUsedViewModel generalUsed, EditContext editContext)
        {
            try
            {
                if (generalUsed.CreateMode)
                {
                    var usedResult = await TenantService.CreateUsageAsync(generalUsed.Form.Model.Map<UpdateUsageRequest>(), cancellationToken: PageCancellationToken);
                    generalUsed.Form.UpdateModel(usedResult.Map<UsedViewModel>());
                    generalUsed.TenantName = usedResult.TenantName;
                    await SetHourPrice(generalUsed);
                    UpdateGeneralModel(generalUsed, usedResult);
                    generalUsed.CreateMode = false;
                    toastService.ShowSuccess("User created.");
                }
                else
                {
                    var usedResult = await TenantService.UpdateUsageAsync(generalUsed.Form.Model.Map<UpdateUsageRequest>(), cancellationToken: PageCancellationToken);
                    generalUsed.Form.UpdateModel(usedResult.Map<UsedViewModel>());
                    UpdateGeneralModel(generalUsed, usedResult);
                    toastService.ShowSuccess("User updated.");
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalUsed.Form.SetFieldError(nameof(generalUsed.Form.Model.TenantName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteUsedAsync(GeneralUsedViewModel generalUsed)
        {
            try
            {
                generalUsed.DeleteAcknowledge = false;
                await TenantService.DeleteUsageAsync(generalUsed.Name, cancellationToken: PageCancellationToken);
                usedList.Remove(generalUsed);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalUsed.Form.SetError(ex.Message);
            }
        }

        private async Task SaveAndDoInvoicingAsync(GeneralUsedViewModel generalUsed)
        {
            try
            {
                (var isValid, var error) = await generalUsed.Form.Submit();
                if (isValid)
                {
                    await DoInvoicingAsync(generalUsed);
                }
                else if (!error.IsNullOrWhiteSpace())
                {
                    toastService.ShowError(error);
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
        }

        private async Task DoInvoicingAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoInvoicing = true }, cancellationToken: PageCancellationToken);
                UpdateGeneralModel(generalUsed, usedResult);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoicingActionButtonDisabled = false;
        }

        private async Task SendInvoiceAgainAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoSendInvoiceAgain = true }, cancellationToken: PageCancellationToken);
                UpdateGeneralModel(generalUsed, usedResult);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoicingActionButtonDisabled = false;
        }

        private async Task DoCreditNoteAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoCreditNote = true }, cancellationToken: PageCancellationToken);
                UpdateGeneralModel(generalUsed, usedResult);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoicingActionButtonDisabled = false;
        }

        private async Task SendCreditNoteAgainAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoSendCreditNoteAgain = true }, cancellationToken: PageCancellationToken);
                UpdateGeneralModel(generalUsed, usedResult);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoicingActionButtonDisabled = false;
        }

        private async Task DoPaymentAgainAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoPaymentAgain = true }, cancellationToken: PageCancellationToken);
                UpdateGeneralModel(generalUsed, usedResult);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoicingActionButtonDisabled = false;
        }

        private async Task DoMarkAsPaidAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, MarkAsPaid = true }, cancellationToken: PageCancellationToken);
                UpdateGeneralModel(generalUsed, usedResult);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoicingActionButtonDisabled = false;
        }

        private async Task DoMarkAsNotPaidAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, MarkAsNotPaid = true }, cancellationToken: PageCancellationToken);
                UpdateGeneralModel(generalUsed, usedResult);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoicingActionButtonDisabled = false;
        }

        private static void UpdateGeneralModel(GeneralUsedViewModel generalUsed, Used usedResult)
        {
            generalUsed.IsInvoiceReady = usedResult.IsInvoiceReady;
            generalUsed.IsUsageCalculated = usedResult.IsUsageCalculated;
            generalUsed.IsInactive = usedResult.IsInactive;
            generalUsed.IsDone = usedResult.IsDone;
            generalUsed.Invoices = usedResult.Invoices;
            generalUsed.HasItems = usedResult.HasItems;
            generalUsed.PaymentStatus = usedResult.PaymentStatus;
        }
    }
}
