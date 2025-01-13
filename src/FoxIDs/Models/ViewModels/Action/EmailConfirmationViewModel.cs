﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class EmailConfirmationViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Confirmation code")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter a email confirmation code.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter a email confirmation code.")]
        public string ConfirmationCode { get; set; }

        public ConfirmationCodeSendStatus ConfirmationCodeSendStatus { get; set; }
    }
}
