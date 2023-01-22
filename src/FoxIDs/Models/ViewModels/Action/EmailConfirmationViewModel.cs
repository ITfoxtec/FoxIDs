﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class EmailConfirmationViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        public bool EnableCreateUser { get; set; }

        [Display(Name = "Confirmation code")]
        [Required]
        [MinLength(Constants.Models.User.EmailConfirmationCodeLength, ErrorMessage = "Please enter a confirmation code.")]
        [MaxLength(Constants.Models.User.EmailConfirmationCodeLength, ErrorMessage = "Please enter a confirmation code.")]
        public string ConfirmationCode { get; set; }

        public EmailConfirmationCodeSendStatus CodeSendStatus { get; set; }
    }
}