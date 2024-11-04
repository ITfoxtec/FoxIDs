using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralUsedViewModel : UsedViewModel
    {
        public GeneralUsedViewModel()
        { }

        public GeneralUsedViewModel(UsedBase used)
        {
            TenantName = used.TenantName;
            PeriodYear = used.PeriodBeginDate.Year;
            PeriodMonth = used.PeriodBeginDate.Month;
            IsUsageCalculated = used.IsUsageCalculated;
            IsInvoiceReady = used.IsInvoiceReady;
            PaymentStatus = used.PaymentStatus;            
        }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool InvoicingActionButtonDisabled { get; set; }

        public string Error { get; set; }

        public PageEditForm<UsedViewModel> Form { get; set; }
    }
}
