using FoxIDs.Logic;
using FoxIDs.Models.ViewModels;
using Microsoft.Extensions.Localization;

namespace FoxIDs
{
    public static class ExceptionExtensions
    {
        public static string GetLocalizedUiMessage(this PasswordPolicyException exception, IStringLocalizer localizer)
        {
            if (exception is SoftChangePasswordException se && se.InnerException is PasswordPolicyException ppe)
            {
                return ppe.GetLocalizedUiMessage(localizer);
            }

            return exception switch
            {
                PasswordLengthException => (exception.PasswordPolicy.CheckComplexity ?
                    localizer[ErrorMessages.PasswordLengthComplex, exception.PasswordPolicy.Length] :
                    localizer[ErrorMessages.PasswordLengthSimple, exception.PasswordPolicy.Length]).Value,
                PasswordMaxLengthException => localizer[ErrorMessages.PasswordMaxLength, exception.PasswordPolicy.MaxLength].Value,
                PasswordBannedCharactersException => localizer[ErrorMessages.PasswordBannedCharacters, exception.PasswordPolicy.BannedCharacters].Value,
                PasswordComplexityException => localizer[ErrorMessages.PasswordComplexity].Value,
                PasswordEmailTextComplexityException => localizer[ErrorMessages.PasswordEmailComplexity].Value,
                PasswordPhoneTextComplexityException => localizer[ErrorMessages.PasswordPhoneComplexity].Value,
                PasswordUsernameTextComplexityException => localizer[ErrorMessages.PasswordUsernameComplexity].Value,
                PasswordUrlTextComplexityException => localizer[ErrorMessages.PasswordUrlComplexity].Value,
                PasswordRiskException => localizer[ErrorMessages.PasswordRisk].Value,
                PasswordHistoryException => localizer[ErrorMessages.PasswordHistory].Value,
                PasswordExpiredException => localizer[ErrorMessages.PasswordExpired].Value,
                _ => localizer[ErrorMessages.ChangePassword].Value
            };
        }
    }
}
