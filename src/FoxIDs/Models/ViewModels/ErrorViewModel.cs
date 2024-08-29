using ITfoxtec.Identity;
using System;
using System.Collections.Generic;

namespace FoxIDs.Models.ViewModels
{
    public class ErrorViewModel : ViewModel
    {
        public DateTimeOffset CreateTime { get; set; }

        public string RequestId { get; set; }
        public string OperationId { get; set; }

        public string ErrorTitle { get; set; }
        public string Error { get; set; }

        public List<string> TechnicalErrors { get; set; }

        public bool ShowErrorTitle => !ErrorTitle.IsNullOrWhiteSpace();
        public bool ShowError => !Error.IsNullOrWhiteSpace();

        public bool ShowRetry { get; set; }
    }
}