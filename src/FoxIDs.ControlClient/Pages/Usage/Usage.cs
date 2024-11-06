﻿using Blazored.Toast.Services;
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

namespace FoxIDs.Client.Pages.Usage
{
    public partial class Usage
    {
        private string usageTenantsHref;
        private string usageSettingsHref;
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
            usageTenantsHref = $"{TenantName}/usagetenants";
            usageSettingsHref = $"{TenantName}/usagesettings";
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

        public bool ShowDoInvoicingAgainButton(GeneralUsedViewModel used) => used.IsUsageCalculated && !used.IsDone;

        public bool ShowSendInvoiceAgainButton(GeneralUsedViewModel used) => used.IsInvoiceReady && (!used.Invoices?.LastOrDefault()?.IsCardPayment != true || used.PaymentStatus == UsagePaymentStatus.Paid);

        public bool ShowDoCreditNoteButton(GeneralUsedViewModel used) => used.IsInvoiceReady && used.PaymentStatus == UsagePaymentStatus.None || used.PaymentStatus.PaymentApiStatusIsGenerallyFailed();

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
                var used = await TenantService.GetUsageAsync(new UsageRequest { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate });
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
            var statusText = $"{(generalUsed.IsUsageCalculated ? ", calculated" : string.Empty)}{(invoiceSendState.HasValue ? $", invoice {invoiceSendState}" : (generalUsed.IsInvoiceReady ? ", invoice created" : string.Empty))}{(generalUsed.PaymentStatus != UsagePaymentStatus.None ? $", payment {generalUsed.PaymentStatus}" : string.Empty)}{(generalUsed.IsDone ? ", DONE" : string.Empty)}";
            var price = invoice?.Price;
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
                used.PeriodBeginDate = new DateOnly(searchUsageForm.Model.PeriodYear, searchUsageForm.Model.PeriodMonth, 1);
                used.PeriodEndDate = used.PeriodBeginDate.AddMonths(1).AddDays(-1);
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

        private async Task DoInvoicingAgainAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoicingActionButtonDisabled = true;
            try
            {
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoInvoicingAgain = true });
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
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoSendInvoiceAgain = true });
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
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoCreditNote = true });
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
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoSendCreditNoteAgain = true });
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
                var usedResult = await TenantService.UsageInvoicingActionAsync(new UsageInvoicingAction { TenantName = generalUsed.TenantName, PeriodBeginDate = generalUsed.PeriodBeginDate, PeriodEndDate = generalUsed.PeriodEndDate, DoPaymentAgain = true });
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
            generalUsed.IsDone = usedResult.IsDone;
            generalUsed.Invoices = usedResult.Invoices;
            generalUsed.PaymentStatus = usedResult.PaymentStatus;
        }
    }
}
