namespace FoxIDs.Models.ViewModels
{
    public static class ErrorMessages
    {
        public const string PasswordLengthComplex = "Please use {0} characters or more with a mix of letters, numbers and symbols.";
        public const string PasswordLengthSimple = "Please use {0} characters or more.";
        public const string PasswordComplexity = "Please use a mix of letters, numbers and symbols";
        public const string PasswordEmailComplexity = "Please do not use the email or parts of it.";
        public const string PasswordPhoneComplexity = "Please do not use the phone number.";
        public const string PasswordUsernameComplexity = "Please do not use the username or parts of it.";
        public const string PasswordUrlComplexity = "Please do not use parts of the URL.";
        public const string PasswordRisk = "The password has previously appeared in a data breach. Please choose a more secure alternative.";
        public const string PasswordNotAccepted = "The password could not be accepted. Please try a different one.";
        public const string AccountLocked = "Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again.";
        public const string WrongPassword = "Wrong password";
        public const string NewPasswordRequired = "Please use a new password.";
        public const string OtpUseNewPhone = "Please use the new one-time password just sent to your phone.";
        public const string OtpUseNewEmail = "Please use the new one-time password just sent to your email.";
        public const string OtpInvalid = "Invalid one-time password, please try one more time.";

        public const string ConfirmationPhoneUseNew = "Please use the new confirmation code just sent to your phone.";
        public const string ConfirmationPhoneUseNewAlt = "Please use the new confirmation code that has just been sent to your phone.";
        public const string ConfirmationPhoneInvalid = "Invalid phone confirmation code, please try one more time.";
        public const string ConfirmationEmailUseNew = "Please use the new confirmation code just sent to your email.";
        public const string ConfirmationEmailUseNewAlt = "Please use the new confirmation code that has just been sent to your email.";
        public const string ConfirmationEmailInvalid = "Invalid email confirmation code, please try one more time.";
        public const string ConfirmationInvalid = "Invalid confirmation code, please try one more time.";

        public const string TwoFactorRegisterInvalidCode = "Invalid code, please try to register the two-factor app one more time.";
        public const string TwoFactorInvalidCode = "Invalid code, please try one more time.";
        public const string TwoFactorRecoveryInvalid = "Invalid recovery code, please try one more time.";
        public const string TwoFactorSmsUseNew = "Please use the new two-factor code just sent to your phone.";
        public const string TwoFactorSmsInvalid = "Invalid two-factor code, please try one more time.";
        public const string TwoFactorEmailUseNew = "Please use the new two-factor code just sent to your email.";
        public const string TwoFactorEmailInvalid = "Invalid two-factor code, please try one more time.";

        public const string ExternalWrongEmailOrPassword = "Wrong email or password.";
        public const string ExternalWrongUsernameOrPassword = "Wrong username or password.";
    }
}
