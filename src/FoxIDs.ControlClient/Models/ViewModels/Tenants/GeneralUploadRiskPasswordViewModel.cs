using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralUploadRiskPasswordViewModel : RiskPasswordInfo
    {
        public const int RiskPasswordMoreThenCount = 100;
        public const int UploadRiskPasswordBlokCount = 2000;
        public const string DefaultRiskPasswordFileStatus = "Drop pwned passwords file here or click to select";
        public const long RiskPasswordMaxFileSize = (long)50 * 1024 * 1024 * 1024; // 50GB

        public GeneralUploadRiskPasswordViewModel()
        { }

        public UploadStates UploadState { get; set; }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<UploadRiskPasswordViewModel> Form { get; set; }

        public string RiskPasswordFileStatus { get; set; } = DefaultRiskPasswordFileStatus;

        public enum UploadStates
        {
            Init,
            Ready,
            Active,
            Done
        }
    }
}
