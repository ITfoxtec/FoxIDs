using ITfoxtec.Identity;

namespace FoxIDs.Models.ViewModels
{
    public class ErrorViewModel : ViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !RequestId.IsNullOrEmpty();
    }
}