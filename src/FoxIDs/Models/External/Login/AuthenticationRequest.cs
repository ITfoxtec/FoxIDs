﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class AuthenticationRequest
    {
        [Required]
        public ExternalLoginUsernameTypes UsernameType { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
