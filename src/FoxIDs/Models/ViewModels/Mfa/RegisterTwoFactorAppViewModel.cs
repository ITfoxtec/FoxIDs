﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class RegisterTwoFactorAppViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool ShowTwoFactorSmsLink { get; set; }

        public bool ShowTwoFactorEmailLink { get; set; }

        public string QrCodeSetupImageUrl { get; set; }

        public string ManualSetupKey { get; set; }

        [Display(Name = "Code")]
        [Required]
        [MaxLength(Constants.Models.User.TwoFactorAppCodeLength, ErrorMessage = "Please enter a valid code")]
        public string AppCode { get; set; }
    }
}
