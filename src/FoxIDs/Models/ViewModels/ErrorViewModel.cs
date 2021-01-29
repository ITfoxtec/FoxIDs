using ITfoxtec.Identity;
using System;

namespace FoxIDs.Models.ViewModels
{
    public class ErrorViewModel : ViewModel
    {
        public DateTimeOffset CreateTime { get; set; }

        public string RequestId { get; set; }

        public string ErrorTitle { get; set; }
        public string Error { get; set; }

        public string TechnicalError { get; set; }

        public bool ShowErrorTitle => !ErrorTitle.IsNullOrWhiteSpace();
        public bool ShowError => !Error.IsNullOrWhiteSpace();
    }
}