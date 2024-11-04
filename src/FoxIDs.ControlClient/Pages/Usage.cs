using Blazored.Toast.Services;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class Usage
    {
        private PageEditForm<FilterUsageViewModel> searchUsageForm;
        private List<GeneralUsedViewModel> usedList = new List<GeneralUsedViewModel>();

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
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                var lastMonth = DateTimeOffset.Now.AddMonths(-1);
                SetGeneralUsageList(await TenantService.FilterUsageAsync(null, lastMonth.Year, lastMonth.Month));
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

        private void SetGeneralUsageList(IEnumerable<UsedBase> filterUsedList)
        {
            var gul = new List<GeneralUsedViewModel>();
            foreach (var u in filterUsedList)
            {
                gul.Add(new GeneralUsedViewModel(u));
            }
            usedList = gul;
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

        public bool ShowSendInvoiceAgainButton(GeneralUsedViewModel used) => used.IsInvoiceReady;

        public bool ShowDoCreditNoteButton(GeneralUsedViewModel used) => used.IsInvoiceReady;

        public bool ShowSendCreditNoteAgainButton(GeneralUsedViewModel used) => !used.IsInvoiceReady && used.Invoices?.LastOrDefault()?.IsCreditNote == true;

        public bool ShowDoPaymentButton(GeneralUsedViewModel used) => used.IsInvoiceReady && used.PaymentStatus.PaymentApiStatusIsGenerallyFailed();

        private async Task ShowUpdateUsageAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.CreateMode = false;
            generalUsed.ShowAdvanced = false;
            generalUsed.Error = null;
            generalUsed.Edit = true;

            try
            {
                var used = await TenantService.GetUsageAsync(new UsageRequest { TenantName = generalUsed.TenantName, PeriodYear = generalUsed.PeriodYear, PeriodMonth = generalUsed.PeriodMonth });
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

        private string UsageInfoText(GeneralUsedViewModel generalUsed)
        {
            var invoice = generalUsed.Invoices?.LastOrDefault();
            var invoiceSendState = invoice?.SendStatus;
            var statusText = generalUsed.IsDone ? ", DONE" : $"{(generalUsed.IsUsageCalculated ? ", usage calculated" : string.Empty)}{(generalUsed.IsInvoiceReady ? ", invoice is ready" : string.Empty)}{(invoiceSendState > 0 ? $", invoice {invoiceSendState}" : string.Empty)}{(generalUsed.PaymentStatus > 0 ? $", payment {generalUsed.PaymentStatus}" : string.Empty)}";
            var price = invoice?.TotalPrice;
            var priceText = $"{(price > 0 ? $", price: {invoice?.Currency}{price}" : string.Empty)}";
            return $"Tenant {generalUsed.TenantName}{statusText}{priceText}";
        }

        private void OnUsageFilterAfterInit(FilterUsageViewModel filterUsage)
        {
            var lastMonth = DateTimeOffset.Now.AddMonths(-1);
            filterUsage.PeriodYear = lastMonth.Year;
            filterUsage.PeriodMonth = lastMonth.Month;
        }

        private async Task OnUsageFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralUsageList(await TenantService.FilterUsageAsync(searchUsageForm.Model.FilterTenantValue, searchUsageForm.Model.PeriodYear, searchUsageForm.Model.PeriodMonth));
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

        private void UsedViewModelAfterInit(GeneralUsedViewModel generalUsed, UsedViewModel used)
        {
            if (generalUsed.CreateMode)
            {
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

        private void AddItem(MouseEventArgs e, List<UsedItem> items, UsedItemTypes type)
        {
            items.Add(new UsedItem { Type = type });
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
                    var usedResult = await TenantService.CreateUsageAsync(generalUsed.Form.Model.Map<UpdateUsageRequest>());
                    generalUsed.Form.UpdateModel(usedResult.Map<UsedViewModel>());
                    generalUsed.CreateMode = false;
                    toastService.ShowSuccess("User created.");
                }
                else
                {
                    var usedResult = await TenantService.UpdateUsageAsync(generalUsed.Form.Model.Map<UpdateUsageRequest>());
                    generalUsed.Form.UpdateModel(usedResult.Map<UsedViewModel>());
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

        private async Task SendInvoiceAgainAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodYear = generalUsed.PeriodYear, PeriodMonth = generalUsed.PeriodMonth, DoSendInvoiceAgain = true });
                generalUsed.IsInvoiceReady = usedResult.IsInvoiceReady;
                generalUsed.Invoices = usedResult.Invoices;
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
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodYear = generalUsed.PeriodYear, PeriodMonth = generalUsed.PeriodMonth, DoCreditNote = true });
                generalUsed.IsInvoiceReady = usedResult.IsInvoiceReady;
                generalUsed.Invoices = usedResult.Invoices;
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
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodYear = generalUsed.PeriodYear, PeriodMonth = generalUsed.PeriodMonth, DoSendCreditNoteAgain = true });
                generalUsed.IsInvoiceReady = usedResult.IsInvoiceReady;
                generalUsed.Invoices = usedResult.Invoices;
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
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodYear = generalUsed.PeriodYear, PeriodMonth = generalUsed.PeriodMonth, DoPaymentAgain = true });
                generalUsed.PaymentStatus = usedResult.PaymentStatus;
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
    }
}
