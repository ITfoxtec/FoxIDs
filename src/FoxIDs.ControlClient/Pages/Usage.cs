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
            var used = new GeneralUsedViewModel();
            used.CreateMode = true;
            used.Edit = true;
            usedList.Add(used);
        }

        private async Task ShowUpdateUsageAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.CreateMode = false;
            generalUsed.ShowAdvanced = false;
            generalUsed.Error = null;
            generalUsed.Edit = true;

            try
            {
                var used = await TenantService.GetUsageAsync(new UsageRequest { TenantName = generalUsed.TenantName, Year = generalUsed.Year, Month = generalUsed.Month });
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
            return $"Tenant: {generalUsed.TenantName}{(generalUsed.InvoiceStatus != UsedInvoiceStatus.None ? $", invoice status: {GetInvoiceStatus(generalUsed)}" : string.Empty)}{(generalUsed.PaymentStatus != UsedPaymentStatus.None ? $", payment status: {GetPaymentStatus(generalUsed)}" : string.Empty)}{(generalUsed.TotalPrice > 0 ? $", total price: €{generalUsed.TotalPrice}" : ", total price: not calculated")}";
        }

        private string GetInvoiceStatus(GeneralUsedViewModel generalUsed)
        {
            switch (generalUsed.InvoiceStatus)
            {
                case UsedInvoiceStatus.InvoiceInitiated:
                    return "initiated";
                case UsedInvoiceStatus.InvoiceSend:
                    return "send";
                case UsedInvoiceStatus.InvoiceFailed:
                    return "failed";
                case UsedInvoiceStatus.CreditNoteInitiated:
                    return "credit note initiated";
                case UsedInvoiceStatus.CreditNoteSend:
                    return "credit note send";
                case UsedInvoiceStatus.CreditNoteFailed:
                    return "credit note failed";
                default:
                    return generalUsed.InvoiceStatus.ToString();
            }
        }

        private string GetPaymentStatus(GeneralUsedViewModel generalUsed)
        {
            switch (generalUsed.PaymentStatus)
            {
                case UsedPaymentStatus.PaymentInitiated:
                    return "initiated";
                case UsedPaymentStatus.PaymentDone:
                    return "done";
                case UsedPaymentStatus.PaymentFailed:
                    return "failed";
                default:
                    return generalUsed.PaymentStatus.ToString();
            }
        }

        private void OnUsageFilterAfterInit(FilterUsageViewModel filterUsage)
        {
            var lastMonth = DateTimeOffset.Now.AddMonths(-1);
            filterUsage.Year = lastMonth.Year;
            filterUsage.Month = lastMonth.Month;
        }

        private async Task OnUsageFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralUsageList(await TenantService.FilterUsageAsync(searchUsageForm.Model.FilterTenantValue, searchUsageForm.Model.Year, searchUsageForm.Model.Month));
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

        private void AddItem(MouseEventArgs e, List<UsedItem> items)
        {
            items.Add(new UsedItem());
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

        private async Task SendInvoiceAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoiceButtonDisabled = true;
            try
            {
                throw new NotImplementedException();

                generalUsed.InvoiceStatus = UsedInvoiceStatus.InvoiceInitiated;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalUsed.InvoiceStatus = UsedInvoiceStatus.InvoiceFailed;
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoiceButtonDisabled = false;
        }

        private async Task SendCreditNoteAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.InvoiceButtonDisabled = true;
            try
            {
                throw new NotImplementedException();

                generalUsed.InvoiceStatus = UsedInvoiceStatus.CreditNoteInitiated;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalUsed.InvoiceStatus = UsedInvoiceStatus.CreditNoteFailed;
                toastService.ShowError(ex.Message);
            }
            generalUsed.InvoiceButtonDisabled = false;
        }

        private async Task ExecutePaymentAsync(GeneralUsedViewModel generalUsed)
        {
            generalUsed.PaymentButtonDisabled = true;
            try
            {
                throw new NotImplementedException();

                generalUsed.PaymentStatus = UsedPaymentStatus.PaymentInitiated;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalUsed.PaymentStatus = UsedPaymentStatus.PaymentFailed;
                toastService.ShowError(ex.Message);
            }
            generalUsed.PaymentButtonDisabled = false;
        }
    }
}
